using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;
using SteamKit2.Internal;
using System.IO;
using System.Windows.Forms;

namespace SteamIrcBot
{
    class SteamAppInfo : ClientMsgHandler
    {
        uint lastChangelist = 0;


        public SteamAppInfo()
        {
            try
            {
                Directory.CreateDirectory( Path.Combine( "cache", "appinfo" ) );
                Directory.CreateDirectory( Path.Combine( "cache", "packageinfo" ) );
            }
            catch ( IOException ex )
            {
                Log.WriteError( "Unable to create appinfo/packageinfo cache directory: {0}", ex.Message );
            }
        }


        public bool GetAppInfo( uint appId, out KeyValue appInfo )
        {
            appInfo = null;

            return false; // todo: implement me
        }

        public bool GetAppName( uint appId, out string name )
        {
            name = null;

            return false; // todo: implement me
        }


        public override void HandleMsg( IPacketMsg packetMsg )
        {
            switch ( packetMsg.MsgType )
            {
                case EMsg.PICSChangesSinceResponse:
                    HandleChangesResponse( packetMsg );
                    break;

                case EMsg.PICSProductInfoResponse:
                    HandleProductInfoResponse( packetMsg );
                    break;
            }
        }


        void HandleChangesResponse( IPacketMsg packetMsg )
        {
            var changesResponse = new ClientMsgProtobuf<CMsgPICSChangesSinceResponse>( packetMsg );

            if ( lastChangelist == changesResponse.Body.current_change_number )
                return;

            lastChangelist = changesResponse.Body.current_change_number;

            if ( changesResponse.Body.app_changes.Count > 0 || changesResponse.Body.package_changes.Count > 0 )
            {
                // this is dirty
                // but because we live in a multiverse that consists of infinite universes
                // i justify this because this method is totally acceptable in at least one of those universes
                Client.GetHandler<SteamApps>().PICSGetProductInfo(
                    apps: changesResponse.Body.app_changes.Select( a => a.appid ),
                    packages: changesResponse.Body.package_changes.Select( p => p.packageid ),
                    onlyPublic: false
                );
            }
        }

        void HandleProductInfoResponse( IPacketMsg packetMsg )
        {
            var productInfo = new ClientMsgProtobuf<CMsgPICSProductInfoResponse>( packetMsg );

            foreach ( var app in productInfo.Body.apps )
            {
                string cacheFile = GetAppCachePath( app.appid );

                try
                {
                    File.WriteAllBytes( cacheFile, app.buffer );
                }
                catch ( IOException ex )
                {
                    Log.WriteError( "SteamAppInfoHandler", "Unable to cache appinfo for appid {0}: {1}", app.appid, ex.Message );
                }
            }

            foreach ( var package in productInfo.Body.packages )
            {
                string cacheFile = GetPackageCachePath( package.packageid );

                try
                {
                    File.WriteAllBytes( cacheFile, package.buffer );
                }
                catch ( IOException ex )
                {
                    Log.WriteError( "SteamAppInfoHandler", "Unable to cache packageinfo for packageid {0}: {1}", package.packageid, ex.Message );
                }
            }
        }


        static string GetAppCachePath( uint appId )
        {
            return Path.Combine( Application.StartupPath, "cache", "appinfo", string.Format( "{0}.txt", appId ) );
        }

        static string GetPackageCachePath( uint packageId )
        {
            return Path.Combine( Application.StartupPath, "cache", "packageinfo", string.Format( "{0}.txt", packageId ) );
        }
    }
}
