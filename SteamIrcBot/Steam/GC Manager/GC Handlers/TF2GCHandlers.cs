using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using SteamKit2;
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


        void OnSaxxyBroadcast( ClientGCMsgProtobuf<CMsgTFSaxxyBroadcast> msg, uint gcAppId )
        {
            IRC.Instance.SendToTag( "tf2-gc", msg.Body.user_name + " has won a saxxy in category: " + msg.Body.category_number );
        }
    }

    class GCWrenchHandler : GCHandler
    {
        public GCWrenchHandler( GCManager manager )
            : base( manager )
        {
            new GCCallback<CMsgTFGoldenWrenchBroadcast>( ( uint )EGCItemMsg.k_EMsgGCGoldenWrenchBroadcast, OnWrenchBroadcast, manager );
        }


        void OnWrenchBroadcast( ClientGCMsgProtobuf<CMsgTFGoldenWrenchBroadcast> msg, uint gcAppId )
        {
            if ( msg.Body.deleted )
            {
                IRC.Instance.SendToTag( "tf2-gc", msg.Body.user_name + " has deleted golden wrench " + msg.Body.wrench_number );
            }
            else
            {
                IRC.Instance.SendToTag( "tf2-gc", msg.Body.user_name + " got golden wrench number " + msg.Body.wrench_number );
            }
        }
    }

    class GCClientItemBroadcastNotificationHandler : GCHandler
    {
        const uint ItemBroadcastNotification = 1096;


        public GCClientItemBroadcastNotificationHandler( GCManager manager )
            : base( manager )
        {
            new GCCallback<CMsgGCTFSpecificItemBroadcast>( ItemBroadcastNotification, OnNotification, manager );
        }


        void OnNotification( ClientGCMsgProtobuf<CMsgGCTFSpecificItemBroadcast> msg, uint gcAppId )
        {
            string itemName = GetItemName( msg.Body.item_def_index );

            if ( msg.Body.was_destruction )
            {
                IRC.Instance.SendToTag( "tf2-gc", "Item notification: {0} has destroyed their {1}!", msg.Body.user_name, itemName );
            }
            else
            {
                IRC.Instance.SendToTag( "tf2-gc", "Item notification: {0} just received a {1}!", msg.Body.user_name, itemName );
            }
        }

        string GetItemName( uint defIndex )
        {
            KeyValue itemsGame = KeyValue.LoadAsText( Path.Combine( Application.StartupPath, "items_game.txt" ) );

            if ( itemsGame == null )
            {
                Log.WriteWarn( "GCClientItemBroadcastNotificationHandler", "Unable to load items_game.txt!" );
                return string.Format( "Unknown Item {0}", defIndex );
            }

            return itemsGame[ "items" ][ defIndex.ToString() ][ "name" ].AsString();
        }
    }


    class GCClientNotificationHandler : GCHandler
    {
        const uint ClientNotification = 1069; // k_EMsgGCApplyConsumableEffects ??

        KeyValue tfEnglish;


        public GCClientNotificationHandler( GCManager manager )
            : base( manager )
        {
            new GCCallback<SteamKit2.GC.TF2.Internal.CMsgGCClientDisplayNotification>( ClientNotification, OnNotification, manager );

            tfEnglish = KeyValue.LoadAsText( Path.Combine( Application.StartupPath, "tf_english.txt" ) );

            if ( tfEnglish == null )
            {
                Log.WriteWarn( "GCClientNotificationHandler", "Unable to load tf_english.txt, localizations will be unavailable!" );
            }
        }


        void OnNotification( ClientGCMsgProtobuf<SteamKit2.GC.TF2.Internal.CMsgGCClientDisplayNotification> msg, uint gcAppId )
        {
            if ( tfEnglish == null )
            {
                Log.WriteWarn( "GCClientNotificationHandler", "Unable to load tf_english.txt, localizations will be unavailable!" );
            }

            string title = LookupToken( msg.Body.notification_title_localization_key );
            string body = LookupToken( msg.Body.notification_body_localization_key );

            var keyValues = msg.Body.body_substring_keys
                .Zip( msg.Body.body_substring_values, ( k, v ) => new { k, v } )
                .ToDictionary( kvp => kvp.k, kvp => kvp.v );

            foreach ( var kvp in keyValues )
            {
                string replaceKey = string.Format( "%{0}%", kvp.Key );

                body = body.Replace( replaceKey, LookupToken( kvp.Value ) );
            }

            IRC.Instance.SendToTag( "tf2-gc", "GC Client Notification: {0}", title );
            IRC.Instance.SendToTag( "tf2-gc", "{0}", body );
        }

        string LookupToken( string tokenName )
        {
            if ( tfEnglish == null )
                return tokenName;

            if ( !tokenName.StartsWith( "#" ) )
                return tokenName;

            var token = tfEnglish[ "Tokens" ][ tokenName.Substring( 1 ) ].AsString();

            if ( string.IsNullOrEmpty( token ) )
                return tokenName;

            token = token.Clean();

            return token;
        }
    }
}
