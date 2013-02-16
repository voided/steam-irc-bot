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
            Trigger = "!pony";
            HelpText = "!pony - pony verb the noun";
        }

        protected override void OnRun( CommandDetails details )
        {
            using ( var webClient = new WebClient() )
            {
                var req = new Request();
                AddRequest( details, req );

                webClient.DownloadStringCompleted += OnDownloadCompleted;
                webClient.DownloadStringAsync( new Uri( "http://areweponyyet.com/?chatty=1" ), req );
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

            IRC.Instance.Send( req.Channel, "{0}: http://areweponyyet.com/{1}", req.Requester.Nickname, e.Result );
        }
    }
}
