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
        uint lastGcVersion = 0;

        public GCSessionHandler( GCManager manager )
            : base( manager )
        {
            new GCCallback<CMsgClientWelcome>( ( uint )EGCBaseClientMsg.k_EMsgGCClientWelcome, OnWelcome, manager );

            // these two callbacks exist to cover the gc message differences between dota and tf2
            // in TF2, it still uses k_EMsgGCClientGoodbye, whereas dota is using k_EMsgGCClientConnectionStatus
            // luckily the message format between the two messages is the same
            new GCCallback<CMsgConnectionStatus>( ( uint )EGCBaseClientMsg.k_EMsgGCClientConnectionStatus, OnConnectionStatus, manager );
            new GCCallback<CMsgConnectionStatus>( 4008 /* tf2's k_EMsgGCClientGoodbye */, OnConnectionStatus, manager );
        }


        void OnWelcome( ClientGCMsgProtobuf<CMsgClientWelcome> msg )
        {
            if ( msg.Body.version != lastGcVersion && lastGcVersion != 0 )
            {
                IRC.Instance.SendAll( "New {0} GC session (version: {1}, previous version: {2})", Steam.Instance.GetAppName( Settings.Current.GCApp ), msg.Body.version, lastGcVersion );
            }
            else
            {
                IRC.Instance.SendAll( "New {0} GC session (version: {1})", Steam.Instance.GetAppName( Settings.Current.GCApp ), msg.Body.version );
            }

            lastGcVersion = msg.Body.version;
        }

        void OnConnectionStatus( ClientGCMsgProtobuf<CMsgConnectionStatus> msg )
        {
            IRC.Instance.SendAll( "{0} GC status: {1}", Steam.Instance.GetAppName( Settings.Current.GCApp ), msg.Body.status );
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
            IRC.Instance.SendAll( "{0} GC system message: {1}", Steam.Instance.GetAppName( Settings.Current.GCApp ), msg.Body.message );
        }
    }

    class GCSchemaHandler : GCHandler
    {
        uint lastSchemaVersion = 0;


        public GCSchemaHandler( GCManager manager )
            : base( manager )
        {
            new GCCallback<CMsgUpdateItemSchema>( ( uint )EGCItemMsg.k_EMsgGCUpdateItemSchema, OnItemSchema, manager );
        }


        void OnItemSchema( ClientGCMsgProtobuf<CMsgUpdateItemSchema> msg )
        {
            if ( lastSchemaVersion != msg.Body.item_schema_version && lastSchemaVersion != 0 )
            {
                IRC.Instance.SendAll( "New {0} GC item schema (version: {1:X4}): {2}", Steam.Instance.GetAppName( Settings.Current.GCApp ), lastSchemaVersion, msg.Body.items_game_url );
            }

            lastSchemaVersion = msg.Body.item_schema_version;
        }
    }
}
