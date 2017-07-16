using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SteamIrcBot
{
    class SlamCommand : Command<SlamCommand.Request>
    {
        public class Request : BaseRequest
        {
        }

        Regex slamRegex1 = new Regex( @"title:(?<quote>')(?<title>(?:\\'|[^'])*)(?<-quote>')", RegexOptions.Compiled | RegexOptions.IgnoreCase );
        // slam.js started using " quotes for strings, so match these titles as well
        Regex slamRegex2 = new Regex( "title:(?<quote>\")(?<title>(?:\\'|[^\"])*)(?<-quote>\")", RegexOptions.Compiled | RegexOptions.IgnoreCase );

        public SlamCommand()
        {
            Triggers.Add( "!slam" );
            HelpText = "!slam - NEO-NEW YORK CHAOS DUNK ADVISORY WARNING! A MEASURED 19.7 MEGAJOULE OF NEGATIVE BBALL PROTONS HAVE BEEN DETECTED. A CHAOS DUNK IS IMMINENT. FIND UNDERGROUND SHELTER IMMEDIATELY. THIS IS NOT A TEST.";
        }

        protected override void OnRun( CommandDetails details )
        {
            using ( var webClient = new WebClient() )
            {
                var req = new Request();
                AddRequest( details, req );

                var uri = new Uri( "http://comeonandsl.am/data/slams.js" );

                webClient.DownloadStringCompleted += OnDownloadCompleted;
                webClient.DownloadStringAsync( uri, req );
            }
        }

        void OnDownloadCompleted( object sender, DownloadStringCompletedEventArgs e )
        {
            var req = GetRequest( r => e.UserState == r );

            if ( req == null )
                return;

            if ( e.Error != null )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request a slam: {1}", req.Requester.Nickname, e.Error.Message );
                return;
            }

            var matches = slamRegex1.Matches( e.Result )
                .OfType<Match>()
                .ToList();

            matches.AddRange( slamRegex2.Matches( e.Result ).OfType<Match>() );

            if ( matches.Count == 0 )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to get slam data", req.Requester.Nickname );
                return;
            }

            int randIndex = new Random().Next( 0, matches.Count );
            Match selected = matches[ randIndex ];

            string title = selected.Groups[ "title" ].Value;

            string url = string.Format( "http://comeonandsl.am/#{0}", Uri.EscapeDataString( title ) );

            IRC.Instance.Send( req.Channel, "{0}: {1}", req.Requester.Nickname, url );
        }
    }
}
