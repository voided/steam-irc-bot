using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;

namespace SteamIrcBot
{
    class DotaGCHandlers : GCHandler
    {
        public DotaGCHandlers( GCManager manager )
            : base( manager )
        {
            new GCCallback<CMsgGCToClientNewBloomTimingUpdated>( (uint)EDOTAGCMsg.k_EMsgGCToClientNewBloomTimingUpdated, OnNewBloomUpdate, manager );
        }

        void OnNewBloomUpdate( ClientGCMsgProtobuf<CMsgGCToClientNewBloomTimingUpdated> msg, uint gcAppId )
        {
            DateTime beastTime = Utils.DateTimeFromUnixTime( msg.Body.next_transition_time );
            TimeSpan timeDiff = beastTime - DateTime.UtcNow;

            bool isTimeKnown = msg.Body.next_transition_time != 1451606400; // Jan 1, 2016

            string changeString = "Unknown";

            if ( isTimeKnown )
            {
                changeString = string.Format( "{0} ({1} UTC)",
                    string.Format( new PluralizeFormatProvider(), "{0:hour/hours}, {1:minute/minutes}, {2:second/seconds}", timeDiff.Hours, timeDiff.Minutes, timeDiff.Seconds ),
                    beastTime
                );
            }

            IRC.Instance.SendToTag( "gc-dota", "{0} Beast Mode: {1}, State changes in {2}, Bonus: {3}, Standby: {4}",
                Steam.Instance.GetAppName( gcAppId ), ( msg.Body.is_active ? "Active" : "Inactive" ), changeString, msg.Body.bonus_amount, msg.Body.standby_duration
            );
        }
    }
}
