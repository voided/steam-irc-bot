using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Net;

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

                lastUpdated[ feed.URL ] = feedItem.PublishDate.DateTime;
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
                if ( !lastUpdated.TryGetValue( feed.URL, out feedLastUpdated ) )
                {
                    // we couldn't get the update time when seeding, but we finally got it now, so lets set it

                    var latestItem = rss.Items.FirstOrDefault();

                    if ( latestItem == null )
                    {
                        Log.WriteWarn( "RSS", "Feed {0} still has no items!", rss.Title.Text );
                        return;
                    }

                    lastUpdated[ feed.URL ] = latestItem.PublishDate.DateTime;

                    // we don't actually know if any of the items are new
                    // so we can't do much else
                    return;
                }

                var newItems = rss.Items
                    .Where( item => item.PublishDate.DateTime > lastUpdated[ feed.URL ] ) // get any feed items newer than the last recorded time we have
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
                    lastUpdated[ feed.URL ] = item.PublishDate.DateTime;
                }
            } );
        }


        SyndicationFeed LoadRSS( SettingsXml.RssFeedXml feedSettings )
        {
            XmlReader reader = null;
            try
            {
                reader = XmlReader.Create( feedSettings.URL );
            }
            catch ( Exception ex )
            {
                Log.WriteWarn( "RSS", "Unable to create xmlreader for feed: {0}", ex.Message );
                return null;
            }

            if ( feedSettings.IsRss10 )
            {
                try
                {
                    return LoadRSS10( feedSettings.URL );
                }
                catch ( Exception ex )
                {
                    Log.WriteWarn( "RSS", "Unable to load RSS 1.0 feed: {0}", ex.Message );
                    return null;
                }
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

        SyndicationFeed LoadRSS10( string url )
        {
            using ( var webClient = new WebClient() )
            using ( var reader = new XmlSanitizingStream( webClient.OpenRead( url ) ) )
            using ( var xmlReader = XmlReader.Create( reader ) )
            {
                XmlDocument doc = new XmlDocument();
                try
                {
                    doc.Load( xmlReader );
                }
                catch ( Exception ex )
                {
                    Log.WriteWarn( "RSS", "Unable to load RSS 1.0 feed: {0}", ex.Message );
                    return null;
                }

                List<SyndicationItem> feedItems = new List<SyndicationItem>();
                SyndicationFeed feed = new SyndicationFeed();
                feed.Items = feedItems;

                XmlNamespaceManager nsManager = new XmlNamespaceManager( doc.NameTable );
                nsManager.AddNamespace( "rss", "http://purl.org/rss/1.0/" );

                feed.Title = new TextSyndicationContent( doc.SelectSingleNode( "//rss:channel/rss:title/text()", nsManager ).Value );

                foreach ( XmlNode node in doc.SelectNodes( "//rss:item", nsManager ) )
                {
                    var item = new SyndicationItem();

                    item.Title = new TextSyndicationContent( node.SelectSingleNode( "./rss:title/text()", nsManager ).Value );
                    item.PublishDate = DateTimeOffset.Parse( node.SelectSingleNode( "./rss:pubDate/text()", nsManager ).Value );

                    item.Links.Add( new SyndicationLink( new Uri( node.SelectSingleNode( "./rss:link/text()", nsManager ).Value ) ) );

                    feedItems.Add( item );
                }

                return feed;
            }
        }
    }
}
