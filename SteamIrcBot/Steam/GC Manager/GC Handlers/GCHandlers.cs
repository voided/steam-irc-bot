using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2.GC;
using SteamKit2.GC.Internal;
using SteamKit2.GC.TF2.Internal;

namespace SteamIrcBot
{
    class GCSessionHandler : GCHandler
    {
        public GCSessionHandler( GCManager manager )
            : base( manager )
        {
            new GCCallback<CMsgClientWelcome>( ( uint )EGCBaseClientMsg.k_EMsgGCClientWelcome, OnWelcome, manager );
            new GCCallback<CMsgConnectionStatus>( ( uint )EGCBaseClientMsg.k_EMsgGCClientConnectionStatus, OnConnectionStatus, manager );
        }


        void OnWelcome( ClientGCMsgProtobuf<CMsgClientWelcome> msg )
        {
            IRC.Instance.SendAll( "New {0} GC session (version: {1})", Settings.Current.GCApp, msg.Body.version );
        }

        void OnConnectionStatus( ClientGCMsgProtobuf<CMsgConnectionStatus> msg )
        {
            IRC.Instance.SendAll( "GC status: {0}", msg.Body.status );
        }
    }

    class GCSystemMsgHandler : GCHandler
    {
        public GCSystemMsgHandler( GCManager manager )
            : base( manager )
        {
            new GCCallback<CMsgSystemBroadcast>( ( uint )EGCBaseMsg.k_EMsgGCSystemMessage, OnSystemMessage, manager );
        }


        void OnSystemMessage( ClientGCMsgProtobuf<CMsgSystemBroadcast> msg )
        {
            IRC.Instance.SendAll( "GC system message: " + msg.Body.message );
        }
    }

    class GCSchemaHandler : GCHandler
    {
        uint lastSchemaVersion;


        public GCSchemaHandler( GCManager manager )
            : base( manager )
        {
            new GCCallback<CMsgUpdateItemSchema>( ( uint )EGCItemMsg.k_EMsgGCUpdateItemSchema, OnItemSchema, manager );
        }


        void OnItemSchema( ClientGCMsgProtobuf<CMsgUpdateItemSchema> msg )
        {
            if ( lastSchemaVersion != msg.Body.item_schema_version )
            {
                lastSchemaVersion = msg.Body.item_schema_version;

#if SERVICE_BUILD // don't be so spammy in dev builds
                IRC.Instance.SendAll( "New GC item schema (version: " + lastSchemaVersion.ToString( "X4" ) + "): " + msg.Body.items_game_url );
#endif
            }
        }
    }
}
