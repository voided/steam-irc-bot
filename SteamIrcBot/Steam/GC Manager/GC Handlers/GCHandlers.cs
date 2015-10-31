using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2.GC;
using SteamKit2.GC.Internal;
using SteamKit2.GC.TF2.Internal;
using SteamKit2.GC.Dota.Internal;
using System.Net;
using System.IO;
using System.Windows.Forms;
using System.Timers;
using System.Collections.Concurrent;
using ProtoBuf;

using CMsgClientWelcome = SteamKit2.GC.Dota.Internal.CMsgClientWelcome;
using CMsgClientHello = SteamKit2.GC.Dota.Internal.CMsgClientHello;
using CMsgSystemBroadcast = SteamKit2.GC.Dota.Internal.CMsgSystemBroadcast;
using CMsgUpdateItemSchema = SteamKit2.GC.Dota.Internal.CMsgUpdateItemSchema;
using CMsgConnectionStatus = SteamKit2.GC.Dota.Internal.CMsgConnectionStatus;

using GCConnectionStatus = SteamKit2.GC.Dota.Internal.GCConnectionStatus;
using EGCBaseMsg = SteamKit2.GC.Dota.Internal.EGCBaseMsg;
using EGCBaseClientMsg = SteamKit2.GC.Dota.Internal.EGCBaseClientMsg;
using EGCItemMsg = SteamKit2.GC.Dota.Internal.EGCItemMsg;

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
            new GCCallback<CMsgClientWelcome>( (uint)EGCBaseClientMsg.k_EMsgGCClientWelcome, OnClientWelcome, manager );
            new GCCallback<CMsgServerWelcome>( (uint)EGCBaseClientMsg.k_EMsgGCServerWelcome, OnServerWelcome, manager );

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

                    if ( gcApp.AppID == 570 )
                    {
                        // todo: this is dirty, we should probably have separate session handlers per-game
                        hello.Body.engine = ESourceEngine.k_ESE_Source2;
                    }

                    Steam.Instance.GameCoordinator.Send( hello, gcApp.AppID );
                }
            }
        }


        void OnClientWelcome( ClientGCMsgProtobuf<CMsgClientWelcome> msg, uint gcAppId )
        {
            SessionInfo info = GetSessionInfo( gcAppId );

            string ircTag = string.Format( "gc-{0}", Settings.Current.GetTagForGCApp( gcAppId ) );

            if ( msg.Body.version != info.Version && info.Version != 0 )
            {
                IRC.Instance.SendToTag( ircTag, "New {0} GC client session (version: {1}, previous version: {2})", Steam.Instance.GetAppName( gcAppId ), msg.Body.version, info.Version );
            }
            else
            {
                IRC.Instance.SendToTag( ircTag, "New {0} GC client session (version: {1})", Steam.Instance.GetAppName( gcAppId ), msg.Body.version );
            }

            info.Version = msg.Body.version;
            info.Status = GCConnectionStatus.GCConnectionStatus_HAVE_SESSION;

            HandleGameData( msg.Body.game_data, gcAppId );
        }

        void OnServerWelcome( ClientGCMsgProtobuf<CMsgServerWelcome> msg, uint gcAppId )
        {
            SessionInfo info = GetSessionInfo( gcAppId );

            string ircTag = string.Format( "gc-{0}", Settings.Current.GetTagForGCApp( gcAppId ) );

            if ( msg.Body.active_version != info.Version && info.Version != 0 )
            {
                IRC.Instance.SendToTag( ircTag, "New {0} GC server session (active version: {1}, previous version: {2}, min allowed: {3})", Steam.Instance.GetAppName( gcAppId ), msg.Body.active_version, info.Version, msg.Body.min_allowed_version );
            }
            else
            {
                IRC.Instance.SendToTag( ircTag, "New {0} GC server session (active version: {1}, min allowed: {2})", Steam.Instance.GetAppName( gcAppId ), msg.Body.active_version, msg.Body.min_allowed_version );
            }

            info.Version = msg.Body.active_version;
            info.Status = GCConnectionStatus.GCConnectionStatus_HAVE_SESSION;
        }

        void OnConnectionStatus( ClientGCMsgProtobuf<CMsgConnectionStatus> msg, uint gcAppId )
        {
            string ircTag = string.Format( "gc-{0}", Settings.Current.GetTagForGCApp( gcAppId ) );

            IRC.Instance.SendToTag( ircTag, "{0} GC status: {1}", Steam.Instance.GetAppName( gcAppId ), msg.Body.status );

            if ( msg.Body.status == GCConnectionStatus.GCConnectionStatus_NO_SESSION_IN_LOGON_QUEUE )
            {
                if ( msg.Body.queue_size > 0 )
                {
                    // this message is somewhat useless if the logon queue is empty, so don't display in that case

                    IRC.Instance.SendToTag( ircTag + "-verbose", "{0} GC logon queue: {1}/{2}, waited {3} of an estimated {4} seconds",
                        Steam.Instance.GetAppName( gcAppId ), msg.Body.queue_position, msg.Body.queue_size, msg.Body.wait_seconds, msg.Body.estimated_wait_seconds_remaining
                    );
                }
            }

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

        void HandleGameData( byte[] gameData, uint gcAppId )
        {
            string ircTag = string.Format( "gc-{0}-verbose", Settings.Current.GetTagForGCApp( gcAppId ) );

            if ( gcAppId != 570 )
            {
                // currently we're only handling dota's extra messages
                return;
            }

            using ( var ms = new MemoryStream( gameData ) )
            {
                CMsgDOTAWelcome dotaWelcome = Serializer.Deserialize<CMsgDOTAWelcome>( ms );


                if ( dotaWelcome.active_events.Count > 0 )
                {
                    string activeEvents = string.Join( ", ", dotaWelcome.active_events );

                    IRC.Instance.SendToTag(
                        ircTag, "{0} GC Active Events: {1}",
                        Steam.Instance.GetAppName( gcAppId ), activeEvents
                    );
                }

                // inject the extra messages back into our gc message manager
                foreach ( var extraMsg in dotaWelcome.extra_messages )
                {
                    Manager.InjectExtraMessage( extraMsg.id, extraMsg.contents, gcAppId );
                }
            }
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
