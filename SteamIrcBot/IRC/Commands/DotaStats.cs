using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;

namespace SteamIrcBot
{
    class DotaStats : Command<DotaStats.Request>
    {
        // from scripts/regions.txt in the VPKs
        enum DotaRegion
        {
            Unspecified = 0,
            USWest = 1,
            USEast = 2,
            Europe = 3,
            Korea = 4,
            Singapore = 5,
            Dubai = 6,
            Australia = 7,
            Stockholm = 8,
            Austria = 9,
            Brazil = 10,
            SouthAfrica = 11,
            PWTelecom = 12,
            PWUnicom = 13,
            Chile = 14,
            Peru = 15,
            India = 16,
        }

        public class Request : BaseRequest
        {
        }

        public DotaStats()
        {
            Triggers.Add( "!dotastats" );
            HelpText = "!dotastats - Requests DOTA matchmaking stats";

            new GCCallback<CMsgDOTAMatchmakingStatsResponse>( (uint)EDOTAGCMsg.k_EMsgGCMatchmakingStatsResponse, OnStats, Steam.Instance.GCManager );
        }

        protected override void OnRun( CommandDetails details )
        {
            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to request dota matchmaking stats: not connected to Steam!", details.Sender.Nickname );
                return;
            }

            var request = new ClientGCMsgProtobuf<CMsgDOTAMatchmakingStatsRequest>( (uint)EDOTAGCMsg.k_EMsgGCMatchmakingStatsRequest );
            Steam.Instance.GameCoordinator.Send( request, 570 );

            AddRequest( details, new Request() );
        }

        void OnStats( ClientGCMsgProtobuf<CMsgDOTAMatchmakingStatsResponse> response, uint appId )
        {
            // don't really have a way to know which gc response maps to which request, so we'll just pull out the latest one
            var req = GetRequest( r => true );

            if ( req == null )
                return;

            var s1Players = response.Body.searching_players_by_group.Select( BuildRegion );
            var s2Players = response.Body.searching_players_by_group_source2.Select( BuildRegion );

            IRC.Instance.Send( req.Channel, "Dota S1 MM Players: {0}", string.Join( ", ", s1Players ) );
            IRC.Instance.Send( req.Channel, "Dota S2 MM Players: {0}", string.Join( ", ", s2Players ) );
        }

        string BuildRegion( uint numPlayers, int index )
        {
            DotaRegion region = (DotaRegion)index;
            return string.Format( "{0}: {1}", region, numPlayers );
        }

        string BuildServers( CMsgSockAddrList addrList, int index )
        {
            DotaRegion region = (DotaRegion)index;
            return string.Format( "{0}: {1}", region, addrList.ip.Count );
        }
    }
}
