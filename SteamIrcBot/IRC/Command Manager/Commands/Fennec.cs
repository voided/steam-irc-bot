using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace SteamIrcBot
{
    class FennecCommand : Command<FennecCommand.Request>
    {
        public class Request : BaseRequest
        {
        }

        public FennecCommand()
        {
            Triggers.Add( "!fennec" );
            HelpText = "!fennec - they're meant to be domesticated";
        }

        protected override void OnRun( CommandDetails details )
        {
            using ( var webClient = new RedirectWebClient() )
            {
                var req = new Request();
                AddRequest( details, req );

                var uri = new Uri( "http://fenne.cc/" );

                webClient.RedirectDownloadStringCompleted += OnDownloadCompleted;
                webClient.DownloadStringAsync( uri, req );
            }
        }

        void OnDownloadCompleted( object sender, RedirectDownloadStringCompletedEventArgs e )
        {
            var req = GetRequest( r => e.EventArgs.UserState == r );

            if ( req == null )
                return;

            if ( e.EventArgs.Error != null )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request a fennec: {1}", req.Requester.Nickname, e.EventArgs.Error.Message );
                return;
            }

            IRC.Instance.Send( req.Channel, "{0}: {1}", req.Requester.Nickname, e.RedirectUri );
        }
    }
}
