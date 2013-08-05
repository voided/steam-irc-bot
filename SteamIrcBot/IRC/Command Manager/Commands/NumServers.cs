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
            HelpText = "!numservers <appid> <filter> [region] / !numservers <filter> - Request a server list from the GMS";

            new JobCallback<SteamMasterServer.QueryCallback>( OnQuery, Steam.Instance.CallbackManager );
        }


        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: filter or AppID and filter arguments required", details.Sender.Nickname );
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

            if ( details.Args.Length >= 2 )
            {
                // specifying appid, filter, and possibly region
                uint appId;
                if ( !uint.TryParse( details.Args[ 0 ], out appId ) )
                {
                    IRC.Instance.Send( details.Channel, "{0}: Invalid AppID argument", details.Sender.Nickname );
                    return;
                }

                query.AppID = appId;
                query.Filter = details.Args[ 1 ];

                if ( details.Args.Length >= 3 )
                {
                    ERegionCode region;
                    if ( !Enum.TryParse<ERegionCode>( details.Args[ 2 ], out region ) )
                    {
                        IRC.Instance.Send( details.Channel, "{0}: Invalid region argument, should be one of: {1}",
                            details.Sender.Nickname, string.Join( ", ", Enum.GetNames( typeof( ERegionCode ) ) )
                        );
                        return;
                    }

                    query.Region = region;
                }
            }
            else
            {
                // just filter
                query.Filter = details.Args[ 0 ];
            }

            var jobId = Steam.Instance.MasterServer.ServerQuery( query );
            AddRequest( details, new Request { JobID = jobId } );
        }


        void OnQuery( SteamMasterServer.QueryCallback callback, JobID jobId )
        {
            var req = GetRequest( r => r.JobID == jobId );

            if ( req == null )
                return;

            if ( callback.Servers.Count <= 20 )
            {
                var response = string.Join( ", ", callback.Servers
                    .Take( 10 )
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
