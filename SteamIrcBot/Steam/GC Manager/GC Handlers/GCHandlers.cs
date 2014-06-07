using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2.GC;
using SteamKit2.GC.Internal;
using SteamKit2.GC.TF2.Internal;
using System.Net;
using System.IO;
using System.Windows.Forms;
using System.Timers;
using System.Collections.Concurrent;

using CMsgClientWelcome = SteamKit2.GC.Internal.CMsgClientWelcome;
using CMsgClientHello = SteamKit2.GC.Internal.CMsgClientHello;
using CMsgSystemBroadcast = SteamKit2.GC.Internal.CMsgSystemBroadcast;
using CMsgUpdateItemSchema = SteamKit2.GC.Internal.CMsgUpdateItemSchema;
using EGCBaseMsg = SteamKit2.GC.Internal.EGCBaseMsg;
using Timer = System.Timers.Timer;

namespace SteamIrcBot
{
    class GCSessionHandler : GCHandler
    {
        class SessionInfo
        {
            public uint Version { get; set; }
            public GCConnectionStatus Status { get; set; }

            public bool HasSession
            {
                get
                {
                    if ( Status == GCConnectionStatus.GCConnectionStatus_HAVE_SESSION )
                        return true; // we have an actual session

                    if ( Status == GCConnectionStatus.GCConnectionStatus_NO_SESSION )
                        return false; // no session

                    if ( Status == GCConnectionStatus.GCConnectionStatus_GC_GOING_DOWN )
                        return false;

                    // otherwise we're likely in logon queue, so assume we'll get a session eventually
                    return true;
                }
            }
        }

        Timer sessTimer = new Timer();

        ConcurrentDictionary<uint, SessionInfo> sessionMap = new ConcurrentDictionary<uint, SessionInfo>();


        public GCSessionHandler( GCManager manager )
            : base( manager )
        {
            new GCCallback<CMsgClientWelcome>( (uint)EGCBaseClientMsg.k_EMsgGCClientWelcome, OnWelcome, manager );

            // these two callbacks exist to cover the gc message differences between dota and tf2
            // in TF2, it still uses k_EMsgGCClientGoodbye, whereas dota is using k_EMsgGCClientConnectionStatus
            // luckily the message format between the two messages is the same
            new GCCallback<CMsgConnectionStatus>( ( uint )EGCBaseClientMsg.k_EMsgGCClientConnectionStatus, OnConnectionStatus, manager );
            new GCCallback<CMsgConnectionStatus>( 4008 /* tf2's k_EMsgGCClientGoodbye */, OnConnectionStatus, manager );

            sessTimer.Interval = TimeSpan.FromSeconds( 30 ).TotalMilliseconds;
            sessTimer.Elapsed += SessionTick;

            sessTimer.Start();
        }

        private void SessionTick( object sender, ElapsedEventArgs e )
        {
            if ( !Steam.Instance.Connected )
                return; // not connected to steam, so we don't tick gc sessions

            foreach ( var gcApp in Settings.Current.GCApps )
            {
                SessionInfo info = GetSessionInfo( gcApp.AppID );

                if ( !info.HasSession )
                {
                    var hello = new ClientGCMsgProtobuf<CMsgClientHello>( (uint)EGCBaseClientMsg.k_EMsgGCClientHello );
                    Steam.Instance.GameCoordinator.Send( hello, gcApp.AppID );
                }
            }
        }


        void OnWelcome( ClientGCMsgProtobuf<CMsgClientWelcome> msg, uint gcAppId )
        {
            SessionInfo info = GetSessionInfo( gcAppId );

            string ircTag = string.Format( "gc-{0}", Settings.Current.GetTagForGCApp( gcAppId ) );

            if ( msg.Body.version != info.Version && info.Version != 0 )
            {
                IRC.Instance.SendToTag( ircTag, "New {0} GC session (version: {1}, previous version: {2})", Steam.Instance.GetAppName( gcAppId ), msg.Body.version, info.Version );
            }
            else
            {
                IRC.Instance.SendToTag( ircTag, "New {0} GC session (version: {1})", Steam.Instance.GetAppName( gcAppId ), msg.Body.version );
            }

            info.Version = msg.Body.version;
            info.Status = GCConnectionStatus.GCConnectionStatus_HAVE_SESSION;
        }

        void OnConnectionStatus( ClientGCMsgProtobuf<CMsgConnectionStatus> msg, uint gcAppId )
        {
            string ircTag = string.Format( "gc-{0}", Settings.Current.GetTagForGCApp( gcAppId ) );

            IRC.Instance.SendToTag( ircTag, "{0} GC status: {1}", Steam.Instance.GetAppName( gcAppId ), msg.Body.status );

            SessionInfo info = GetSessionInfo( gcAppId );

            info.Status = msg.Body.status;
        }

        SessionInfo GetSessionInfo( uint gcAppId )
        {
            SessionInfo info;

            if ( sessionMap.TryGetValue( gcAppId, out info ) )
            {
                return info;
            }

            info = new SessionInfo();
            info.Status = GCConnectionStatus.GCConnectionStatus_NO_SESSION;

            sessionMap[ gcAppId ] = info;

            return info;
        }
    }

    class GCSystemMsgHandler : GCHandler
    {
        public GCSystemMsgHandler( GCManager manager )
            : base( manager )
        {
            new GCCallback<CMsgSystemBroadcast>( (uint)EGCBaseMsg.k_EMsgGCSystemMessage, OnSystemMessage, manager );
        }


        void OnSystemMessage( ClientGCMsgProtobuf<CMsgSystemBroadcast> msg, uint gcAppId )
        {
            string ircTag = string.Format( "gc-{0}", Settings.Current.GetTagForGCApp( gcAppId ) );

            IRC.Instance.SendToTag( ircTag, "{0} GC system message: {1}", Steam.Instance.GetAppName( gcAppId ), msg.Body.message );
        }
    }

    class GCSchemaHandler : GCHandler
    {
        uint lastSchemaVersion = 0;


        public GCSchemaHandler( GCManager manager )
            : base( manager )
        {
            new GCCallback<CMsgUpdateItemSchema>( (uint)EGCItemMsg.k_EMsgGCUpdateItemSchema, OnItemSchema, manager );
        }


        void OnItemSchema( ClientGCMsgProtobuf<CMsgUpdateItemSchema> msg, uint gcAppId )
        {
            string ircTag = string.Format( "gc-{0}", Settings.Current.GetTagForGCApp( gcAppId ) );

            if ( lastSchemaVersion != msg.Body.item_schema_version && lastSchemaVersion != 0 )
            {
                IRC.Instance.SendToTag( ircTag, "New {0} GC item schema (version: {1:X4}): {2}", Steam.Instance.GetAppName( gcAppId ), lastSchemaVersion, msg.Body.items_game_url );
            }

            lastSchemaVersion = msg.Body.item_schema_version;

            using ( var webClient = new WebClient() )
            {
                string itemsGameFile = string.Format( "items_game_{0}.txt", gcAppId );

                webClient.DownloadFileAsync( new Uri( msg.Body.items_game_url ), Path.Combine( Application.StartupPath, itemsGameFile ) );
            }
        }
    }
}
