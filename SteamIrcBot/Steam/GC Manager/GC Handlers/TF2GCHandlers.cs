using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;
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

    class GCClientNotificationHandler : GCHandler
    {
        const uint ClientNotification = 1069; // k_EMsgGCApplyConsumableEffects ??

        KeyValue tfEnglish;


        public GCClientNotificationHandler( GCManager manager )
            : base( manager )
        {
            new GCCallback<CMsgGCClientDisplayNotification>( ClientNotification, OnNotification, manager );

            tfEnglish = KeyValue.LoadAsText( Path.Combine( Application.StartupPath, "tf_english.txt" ) );

            if ( tfEnglish == null )
            {
                Log.WriteWarn( "GCClientNotificationHandler", "Unable to load tf_english.txt, localizations will be unavailable!" );
            }
        }


        void OnNotification( ClientGCMsgProtobuf<CMsgGCClientDisplayNotification> msg )
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

            IRC.Instance.SendAll( "GC Client Notification: {0}", title );
            IRC.Instance.SendAll( "{0}", body );
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

            token = Regex.Replace( token, @"\s+|\p{C}+", " " );

            return token;
        }
    }
}
