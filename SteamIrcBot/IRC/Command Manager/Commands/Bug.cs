using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SteamIrcBot
{
    class BugCommand : Command<BugCommand.Request>
    {
        public class Request : BaseRequest
        {
            public string Url { get; set; }
        }

        public BugCommand()
        {
            Triggers.Add( "!bug" );
            Triggers.Add( "!mozbug" );
            Triggers.Add( "!mug" );

            HelpText = "!bug/!mozbug <bugid> - Link to a AM or mozilla bug";
        }

        protected override void OnRun( CommandDetails details )
        {
            bool isAMBug = string.Equals( details.Trigger, "!bug", StringComparison.OrdinalIgnoreCase );

            if ( isAMBug )
            {
                if ( IRC.Instance.IsUserOnChannel( details.Channel, "yakbot" ) )
                    return; // glory to the yak
            }

            if ( details.Args.Length < 1 )
            {
                IRC.Instance.Send( details.Channel, "{0}: {1}", details.Sender.Nickname, HelpText );
                return;
            }

            string bugOrAlias = string.Join( " ", details.Args );
            bugOrAlias = HttpUtility.UrlEncode( bugOrAlias );

            using ( var webClient = new WebClient() )
            {
                var req = new Request();
                AddRequest( details, req );

                webClient.DownloadStringCompleted += OnDownloadCompleted;

                req.Url = "https://bugzilla.mozilla.org/show_bug.cgi?id={0}";

                if ( isAMBug )
                    req.Url = "https://bugs.alliedmods.net/show_bug.cgi?id={0}";

                req.Url = string.Format( req.Url, bugOrAlias );

                webClient.DownloadStringAsync( new Uri( req.Url ), req );
            }
        }

        void OnDownloadCompleted( object sender, DownloadStringCompletedEventArgs e )
        {
            var req = GetRequest( r => e.UserState == r );

            if ( req == null )
                return;

            if ( e.Error != null )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request info about bug: {1}", req.Requester.Nickname, e.Error.Message );
                return;
            }

            HtmlDocument htmlDoc = new HtmlDocument();
            try
            {
                htmlDoc.LoadHtml( e.Result );
            }
            catch ( Exception ex )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request info about bug: there was an error parsing the response", req.Requester.Nickname );
                Log.WriteWarn( "BugCommand", "Unable to parse response from bugzilla server: {0}", ex );
                return;
            }

            var textNode = htmlDoc.DocumentNode.SelectSingleNode( "//span[@id='summary_alias_container']" );

            if ( textNode == null || textNode.InnerText == null )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request info about bug: the returned response was invalid", req.Requester.Nickname );
                return;
            }

            string cleanTitle = textNode.InnerText.Clean().Trim();
            cleanTitle = HttpUtility.HtmlDecode( cleanTitle );

            IRC.Instance.Send( req.Channel, "{0}: {1} - {2}", req.Requester.Nickname, req.Url, cleanTitle );
        }
    }
}
