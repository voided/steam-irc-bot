using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;

namespace SteamIrcBot
{
    class NumServersCommand : Command<NumServersCommand.Request>
    {
        public class Request : BaseRequest
        {
            public JobID JobID { get; set; }
        }


        public NumServersCommand()
        {
            Triggers.Add( "!numservers" );
            HelpText = "!numservers <filter> - Request a server list from the GMS";

            Steam.Instance.CallbackManager.Subscribe<SteamMasterServer.QueryCallback>( OnQuery );
        }


        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: filter argument required", details.Sender.Nickname );
                return;
            }

            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to request server count: not connected to Steam!", details.Sender.Nickname );
                return;
            }

            var query = new SteamMasterServer.QueryDetails
            {
                Region = ERegionCode.World,
            };

            query.Filter = string.Join( " ", details.Args );

            var jobId = Steam.Instance.MasterServer.ServerQuery( query );
            AddRequest( details, new Request { JobID = jobId } );
        }


        void OnQuery( SteamMasterServer.QueryCallback callback )
        {
            var req = GetRequest( r => r.JobID == callback.JobID );

            if ( req == null )
                return;

            if ( callback.Servers.Count == 0 )
            {
                IRC.Instance.Send( req.Channel, "{0}: No servers", req.Requester.Nickname );
            }
            else if ( callback.Servers.Count <= 20 )
            {
                var response = string.Join( ", ", callback.Servers
                    .Take( 20 )
                    .Select( s => string.Format( "{0} ({1})", s.EndPoint, s.AuthedPlayers ) ) );

                IRC.Instance.Send( req.Channel, "{0}: {1}", req.Requester.Nickname, response );
            }
            else
            {
                const int MAX_SERVERS = 5000; // the maximum amount of servers te GMS will reply with

                string response = string.Format( callback.Servers.Count == MAX_SERVERS ? ">{0}" : "{0}", callback.Servers.Count );

                IRC.Instance.Send( req.Channel, "{0}: {1} servers", req.Requester.Nickname, response );
            }
        }
    }
}
