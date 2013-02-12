using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2.GC;
using SteamKit2.GC.Internal;
using SteamKit2.GC.TF2.Internal;

namespace SteamIrcBot
{
    class GCSaxxyHandler : GCHandler
    {
        public GCSaxxyHandler( GCManager manager )
            : base( manager )
        {
            new GCCallback<CMsgTFSaxxyBroadcast>( ( uint )EGCItemMsg.k_EMsgGCSaxxyBroadcast, OnSaxxyBroadcast, manager );
        }

        void OnSaxxyBroadcast( ClientGCMsgProtobuf<CMsgTFSaxxyBroadcast> msg )
        {
            IRC.Instance.SendAll( msg.Body.user_name + " has won a saxxy in category: " + msg.Body.category_number );
        }
    }

    class GCWrenchHandler : GCHandler
    {
        public GCWrenchHandler( GCManager manager )
            : base( manager )
        {
            new GCCallback<CMsgTFGoldenWrenchBroadcast>( ( uint )EGCItemMsg.k_EMsgGCGoldenWrenchBroadcast, OnWrenchBroadcast, manager );
        }

        void OnWrenchBroadcast( ClientGCMsgProtobuf<CMsgTFGoldenWrenchBroadcast> msg )
        {
            if ( msg.Body.deleted )
            {
                IRC.Instance.SendAll( msg.Body.user_name + " has deleted golden wrench " + msg.Body.wrench_number );
            }
            else
            {
                IRC.Instance.SendAll( msg.Body.user_name + " got golden wrench number " + msg.Body.wrench_number );
            }
        }
    }
}
