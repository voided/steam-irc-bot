using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace SteamIrcBot
{
    class PonyCommand : Command<PonyCommand.Request>
    {
        public class Request : BaseRequest
        {
        }

        public PonyCommand()
        {
            Triggers.Add( "!pony" );
            HelpText = "!pony - pony verb the noun";
        }

        protected override void OnRun( CommandDetails details )
        {
            var pony = details.Args
                .FirstOrDefault() ?? "";

            using ( var webClient = new WebClient() )
            {
                var req = new Request();
                AddRequest( details, req );

                var uri = new Uri( string.Format( "https://areweponyyet.com/?chatty=1&pony={0}", pony ) );

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
                IRC.Instance.Send( req.Channel, "{0}: Unable to request a pony video: {1}", req.Requester.Nickname, e.Error.Message );
                return;
            }

            IRC.Instance.Send( req.Channel, "{0}: https://areweponyyet.com/{1}", req.Requester.Nickname, e.Result );
        }
    }
}
