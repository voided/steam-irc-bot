using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using System.Net;

namespace SteamIrcBot
{
    class FindServerCommand : Command<FindServerCommand.Request>
    {
        public class Request : BaseRequest
        {
            public JobID JobID { get; set; }

            public IPAddress SearchIP { get; set; }
        }


        public FindServerCommand()
        {
            Triggers.Add( "!findserver" );
            HelpText = "!findserver <ip> <geoip|\"none\"> <region> <filter> - Determines what position a server is in the GMS's server list";

            Steam.Instance.CallbackManager.Subscribe<SteamMasterServer.QueryCallback>( OnQuery );
        }


        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length < 3 )
            {
                base.ShowHelp( details );
                return;
            }

            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to find server: not connected to Steam!", details.Sender.Nickname );
                return;
            }

            IPAddress search;

            if ( !IPAddress.TryParse( details.Args[ 0 ], out search ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid search IP address.", details.Sender.Nickname );
                base.ShowHelp( details );
                return;
            }

            IPAddress geo;

            if ( !IPAddress.TryParse( details.Args[ 1 ], out geo ) )
            {
                geo = null;

                if ( !string.Equals( details.Args[ 1 ], "none", StringComparison.OrdinalIgnoreCase ) )
                {
                    IRC.Instance.Send( details.Channel, "{0}: Invalid geo IP address.", details.Sender.Nickname );
                    base.ShowHelp( details );
                    return;
                }
            }

            ERegionCode regionCode;

            if ( !Enum.TryParse( details.Args[ 2 ], true, out regionCode ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid region code, should be one of: {1}",
                    details.Sender.Nickname, string.Join( ", ", Enum.GetNames( typeof( ERegionCode ) ) )
                );
                base.ShowHelp( details );
                return;
            }

            var query = new SteamMasterServer.QueryDetails
            {
                GeoLocatedIP = geo,
                Region = regionCode,
            };

            query.Filter = string.Join( " ", details.Args.Skip( 3 ) );

            var jobId = Steam.Instance.MasterServer.ServerQuery( query );
            AddRequest( details, new Request { JobID = jobId, SearchIP = search } );
        }


        void OnQuery( SteamMasterServer.QueryCallback callback )
        {
            var req = GetRequest( r => r.JobID == callback.JobID );

            if ( req == null )
                return;

            if ( callback.Servers.Count == 0 )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to find {1}: No servers in response", req.Requester.Nickname, req.SearchIP );
                return;
            }

            int index = callback.Servers.ToList().FindIndex( serv => serv.EndPoint.Address.Equals( req.SearchIP ) );

            if ( index == -1 )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to find {1}: Not found in server response", req.Requester.Nickname, req.SearchIP );
                return;
            }

            IRC.Instance.Send( req.Channel, "{0}: Found {1} at index {2}, with {3} authed players", req.Requester.Nickname, req.SearchIP, index, callback.Servers[ index ].AuthedPlayers );
        }
    }
}
