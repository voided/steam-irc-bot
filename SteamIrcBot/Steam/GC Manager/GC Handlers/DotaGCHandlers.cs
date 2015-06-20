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

        UGCHandler ugcHandler;


        public GCNewBloomHandler( GCManager manager )
            : base( manager )
        {
            Instance = this;

            new GCCallback<CMsgGCToClientNewBloomTimingUpdated>( (uint)EDOTAGCMsg.k_EMsgGCToClientNewBloomTimingUpdated, OnNewBloomUpdate, manager );
            new GCCallback<CMsgGCToClientTopCustomGamesList>( (uint)EDOTAGCMsg.k_EMsgGCToClientTopCustomGamesList, OnTopCustomGames, manager );

            ugcHandler = Steam.Instance.SteamManager.GetHandler<UGCHandler>();
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

            var games = msg.Body.top_custom_games.Take( 20 );

            // convert our pubfile ids into user friendly names
            var gameInfos = games.Select( pubFile =>
            {
                string name;

                if ( !ugcHandler.LookupUGCName( pubFile, out name ) )
                {
                    // couldn't look up a name, resort to displaying the pub file
                    return pubFile.ToString();
                }

                return name;
            } );

            IRC.Instance.SendToTag( "gc-dota-verbose", "{0} Top customs: {1}", Steam.Instance.GetAppName( gcAppId ), string.Join( ", ", gameInfos ) );
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
