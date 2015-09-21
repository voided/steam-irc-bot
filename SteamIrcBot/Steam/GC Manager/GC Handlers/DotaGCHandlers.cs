using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;

namespace SteamIrcBot
{
    class DotaGCNewBloomHandler : GCHandler
    {
        public static DotaGCNewBloomHandler Instance { get; private set; }

        CMsgGCToClientNewBloomTimingUpdated lastInfo;


        public DotaGCNewBloomHandler( GCManager manager )
            : base( manager )
        {
            Instance = this;

            new GCCallback<CMsgGCToClientNewBloomTimingUpdated>( (uint)EDOTAGCMsg.k_EMsgGCToClientNewBloomTimingUpdated, OnNewBloomUpdate, manager );
        }

        void OnNewBloomUpdate( ClientGCMsgProtobuf<CMsgGCToClientNewBloomTimingUpdated> msg, uint gcAppId )
        {
            lastInfo = msg.Body;

            IRC.Instance.SendToTag( "gc-dota-verbose", "{0} {1}", Steam.Instance.GetAppName( gcAppId ), GetDisplay() );
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

    class DotaGCTopGamesHandler : GCHandler
    {
        public static DotaGCTopGamesHandler Instance { get; private set; }


        CMsgGCTopCustomGamesList cachedGames;

        UGCHandler ugcHandler;


        public DotaGCTopGamesHandler( GCManager manager )
            : base ( manager )
        {
            Instance = this;

            new GCCallback<CMsgGCTopCustomGamesList>( (uint)EDOTAGCMsg.k_EMsgGCTopCustomGamesList, OnTopCustomGames, manager );

            ugcHandler = Steam.Instance.SteamManager.GetHandler<UGCHandler>();
        }


        void OnTopCustomGames( ClientGCMsgProtobuf<CMsgGCTopCustomGamesList> msg, uint gcAppId )
        {
            cachedGames = msg.Body;

            string displayMsg = GetDisplay();

            IRC.Instance.SendToTag( "gc-dota-verbose", "{0} {1}", Steam.Instance.GetAppName( gcAppId ), displayMsg );
        }


        public string GetDisplay()
        {
            if ( cachedGames == null )
            {
                // nothing cached yet
                return "Top customs: unknown";
            }

            int numGames = cachedGames.top_custom_games.Count;

            if ( numGames == 0 )
            {
                // nothing useful to display
                return "Top customs: none";
            }

            var games = cachedGames.top_custom_games.Take( 20 );

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

            return string.Format( "Top customs: {0}", string.Join( ", ", gameInfos ) );
        }
    }
}
