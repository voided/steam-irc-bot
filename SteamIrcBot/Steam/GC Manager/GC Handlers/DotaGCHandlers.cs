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
        }

        void OnNewBloomUpdate( ClientGCMsgProtobuf<CMsgGCToClientNewBloomTimingUpdated> msg, uint gcAppId )
        {
            lastInfo = msg.Body;

            IRC.Instance.SendToTag( "gc-dota", "{0} {1}", Steam.Instance.GetAppName( gcAppId ), GetDisplay() );
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

            bool isTimeKnown = lastInfo.next_transition_time != 1451606400; // Jan 1, 2016

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
