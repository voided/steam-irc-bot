using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;

namespace SteamIrcBot
{
    class GCNewBloomHandler : GCHandler
    {
        public static GCNewBloomHandler Instance { get; private set; }

        CMsgGCToClientNewBloomTimingUpdated lastInfo;


        public GCNewBloomHandler( GCManager manager )
            : base( manager )
        {
            Instance = this;

            new GCCallback<CMsgGCToClientNewBloomTimingUpdated>( (uint)EDOTAGCMsg.k_EMsgGCToClientNewBloomTimingUpdated, OnNewBloomUpdate, manager );
            new GCCallback<CMsgGCToClientTopCustomGamesList>( (uint)EDOTAGCMsg.k_EMsgGCToClientTopCustomGamesList, OnTopCustomGames, manager );
        }

        void OnNewBloomUpdate( ClientGCMsgProtobuf<CMsgGCToClientNewBloomTimingUpdated> msg, uint gcAppId )
        {
            lastInfo = msg.Body;

            IRC.Instance.SendToTag( "gc-dota-verbose", "{0} {1}", Steam.Instance.GetAppName( gcAppId ), GetDisplay() );
        }

        void OnTopCustomGames( ClientGCMsgProtobuf<CMsgGCToClientTopCustomGamesList> msg, uint gcAppId )
        {
            int numGames = msg.Body.top_custom_games.Count;

            if ( numGames == 0 )
            {
                // nothing useful to display
                return;
            }

            if ( numGames <= 20 )
            {
                IRC.Instance.SendToTag( "gc-dota-verbose", "{0} Top custom games: {1}", Steam.Instance.GetAppName( gcAppId ), string.Join( ", ", msg.Body.top_custom_games ) );
            }
            else
            {
                var games = msg.Body.top_custom_games.Take( 20 );

                IRC.Instance.SendToTag( "gc-dota-verbose", "{0} Top custom games: {1}, and {2} more...", Steam.Instance.GetAppName( gcAppId ), string.Join( ", ", games ), numGames - 20 );
            }
        }

        public string GetDisplay()
        {
            if ( lastInfo == null )
            {
                // don't have enough info yet
                return "Beast Mode: Unknown";
            }

            DateTime beastTime = Utils.DateTimeFromUnixTime( lastInfo.next_transition_time );
            TimeSpan timeDiff = beastTime - DateTime.UtcNow;

            bool isTimeKnown = lastInfo.next_transition_time != 0;

            string changeString = "Unknown";

            if ( isTimeKnown )
            {
                changeString = string.Format( "{0} ({1} UTC)",
                    string.Format( new PluralizeFormatProvider(), "{0:hour/hours}, {1:minute/minutes}, {2:second/seconds}", timeDiff.Hours, timeDiff.Minutes, timeDiff.Seconds ),
                    beastTime
                );
            }

            return string.Format( "Beast Mode: {0}, State changes in {1}, Bonus: {2}, Standby: {3}",
                ( lastInfo.is_active ? "Active" : "Inactive" ), changeString, lastInfo.bonus_amount, lastInfo.standby_duration
            );
        }
    }
}
