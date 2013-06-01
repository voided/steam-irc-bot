using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SteamIrcBot
{
    class RSS
    {
        public static RSS _instance = new RSS();
        public static RSS Instance { get { return _instance; } }


        ConcurrentDictionary<string, DateTime> lastUpdated = new ConcurrentDictionary<string, DateTime>();

        DateTime nextUpdate = DateTime.MaxValue;


        RSS()
        {
        }


        public void Start()
        {
            Log.WriteInfo( "RSS", "Seeding feeds..." );

            // seed our feed map with the last publish time for all our feeds
            // this will be used to determine if anything new is posted
            Parallel.ForEach( Settings.Current.RssFeeds, feed =>
            {
                var rssFeed = LoadRSS( feed );

                if ( rssFeed == null )
                    return;

                var feedItem = rssFeed.Items.FirstOrDefault();

                if ( feedItem == null )
                {
                    Log.WriteWarn( "RSS", "Feed {0} has no items!", rssFeed.Title.Text );
                    return;
                }

                lastUpdated[ feed ] = feedItem.PublishDate.DateTime;
            } );

            Log.WriteInfo( "RSS", "Done! Beginning updates in 1 minute." );
            nextUpdate = DateTime.Now + TimeSpan.FromMinutes( 1 );
        }

        public void Stop()
        {
            nextUpdate = DateTime.MaxValue;
        }


        public void Tick()
        {
            if ( nextUpdate >= DateTime.Now )
                return;

            nextUpdate = DateTime.Now + TimeSpan.FromMinutes( 5 );

            Parallel.ForEach( Settings.Current.RssFeeds, feed =>
            {
                var rss = LoadRSS( feed );

                if ( rss == null )
                    return; // rip

                DateTime feedLastUpdated;
                if ( !lastUpdated.TryGetValue( feed, out feedLastUpdated ) )
                {
                    // we couldn't get the update time when seeding, but we finally got it now, so lets set it

                    var latestItem = rss.Items.FirstOrDefault();

                    if ( latestItem == null )
                    {
                        Log.WriteWarn( "RSS", "Feed {0} still has no items!", rss.Title.Text );
                        return;
                    }

                    lastUpdated[ feed ] = latestItem.PublishDate.DateTime;

                    // we don't actually know if any of the items are new
                    // so we can't do much else
                    return;
                }

                var newItems = rss.Items
                    .Where( item => item.PublishDate.DateTime > lastUpdated[ feed ] ) // get any feed items newer than the last recorded time we have
                    .OrderBy( item => item.PublishDate.DateTime ); // sort them oldest to newest

                foreach ( var item in newItems )
                {
                    string newsUrl = "";

                    var link = item.Links.FirstOrDefault();
                    if ( link != null && link.Uri != null )
                    {
                        newsUrl = link.Uri.ToString();
                    }

                    IRC.Instance.SendAll( "{0} News: {1} - {2}", rss.Title.Text, item.Title.Text, newsUrl );

                    // guaranteed to give us the most recent item at the last iteration because of our sort order
                    lastUpdated[ feed ] = item.PublishDate.DateTime;
                }
            } );
        }


        SyndicationFeed LoadRSS( string url )
        {
            XmlReader reader = null;
            try
            {
                reader = XmlReader.Create( url );
            }
            catch ( Exception ex )
            {
                Log.WriteWarn( "RSS", "Unable to create xmlreader for feed: {0}", ex.Message );
                return null;
            }

            SyndicationFeed feed = null;
            try
            {
                feed = SyndicationFeed.Load( reader );
            }
            catch ( Exception ex )
            {
                Log.WriteWarn( "RSS", "Unable to load syndication feed: {0}", ex.Message );
                return null;
            }

            return feed;
        }
    }
}
