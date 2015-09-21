using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.TF2.Internal;

namespace SteamIrcBot
{
    class TF2WarStats : Command<TF2WarStats.Request>
    {
        public class Request : BaseRequest
        {
        }

        public TF2WarStats()
        {
            Triggers.Add( "!warstats" );
            HelpText = "!warstats - Requests TF2 war stats for Spy vs Engi";

            new GCCallback<CGCMsgGC_SpyVsEngyWar_GlobalStatsResponse>( (uint)ETFGCMsg.k_EMsgGC_SpyVsEngyWar_GlobalStatsResponse, OnStats, Steam.Instance.GCManager );
        }

        protected override void OnRun( CommandDetails details )
        {
            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to request tf2 war stats: not connected to Steam!", details.Sender.Nickname );
                return;
            }

            var request = new ClientGCMsgProtobuf<CGCMsgGC_SpyVsEngyWar_RequestGlobalStats>( (uint)ETFGCMsg.k_EMsgGC_SpyVsEngyWar_RequestGlobalStats );
            Steam.Instance.GameCoordinator.Send( request, 440 );

            AddRequest( details, new Request() );
        }

        void OnStats( ClientGCMsgProtobuf<CGCMsgGC_SpyVsEngyWar_GlobalStatsResponse> response, uint appId )
        {
            // don't really have a way to know which gc response maps to which request, so we'll just pull out the latest one
            var req = GetRequest( r => true );

            if ( req == null )
                return;

            IRC.Instance.Send( req.Channel, "Engi Score: {0} | Spy Score: {1}", response.Body.engy_score, response.Body.spy_score );
        }
    }
}
