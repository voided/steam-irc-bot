using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using SteamKit2;
using SteamKit2.Internal;
using System.Diagnostics;

namespace SteamIrcBot
{
    class SteamAppInfo : ClientMsgHandler
    {
        class AppCacheEntry
        {
            public uint AppID { get; set; }
            public bool IsGame { get; set; }
        }

        uint lastChangelist = 0;

        Dictionary<string, AppCacheEntry> appNameCache = new Dictionary<string, AppCacheEntry>();


        public SteamAppInfo()
        {
            try
            {
                Directory.CreateDirectory( Path.Combine( "cache", "appinfo" ) );
                Directory.CreateDirectory( Path.Combine( "cache", "packageinfo" ) );
            }
            catch ( IOException ex )
            {
                Log.WriteError( "SteamAppInfo", "Unable to create appinfo/packageinfo cache directory: {0}", ex.Message );
            }

            CacheApps();
        }


        public bool GetAppInfo( uint appId, out KeyValue appInfo )
        {
            appInfo = KeyValue.LoadAsText( GetAppCachePath( appId ) );

            // cache off the name
            if ( appInfo != null )
            {
                string name = appInfo[ "common" ][ "name" ].AsString();
                string type = appInfo[ "common" ][ "type" ].AsString();

                bool isGame = string.Equals( type, "game", StringComparison.OrdinalIgnoreCase );

                if ( name != null )
                {
                    appNameCache[ name ] = new AppCacheEntry { AppID = appId, IsGame = isGame };
                }
            }

            return appInfo != null;
        }

        public bool GetAppName( uint appId, out string name )
        {
            name = null;

            KeyValue appInfo;
            if ( !GetAppInfo( appId, out appInfo ) )
                return false;

            name = appInfo[ "common" ][ "name" ].AsString();

            return name != null;
        }

        public bool FindApp( string search, out uint appId, bool gamesOnly )
        {
            appId = 0;

            var appMatches = appNameCache
                .Where( kvp => kvp.Key.IndexOf( search, StringComparison.OrdinalIgnoreCase ) != -1 )
                .ToList();

            if ( appMatches.Count == 0 )
            {
                return false;
            }

            var appList = appMatches.Select( kvp => kvp.Value );

            if ( gamesOnly )
            {
                // todo: appease azuisleet with something better than searching for only games
                appList = appList.Where( app => app.IsGame );
            }

            AppCacheEntry searchResult = appList.FirstOrDefault();

            if ( searchResult != null )
            {
                appId = searchResult.AppID;
                return true;
            }

            return false;
        }

        public void CacheApps()
        {
            // cache all the app names we know about

            Stopwatch stopWatch = Stopwatch.StartNew();

            foreach ( string file in Directory.EnumerateFiles( Path.Combine( "cache", "appinfo" ) ) )
            {
                string fileAppId = Path.GetFileNameWithoutExtension( file );

                uint appId;

                if ( !uint.TryParse( fileAppId, out appId ) )
                    return;

                KeyValue ignored;
                GetAppInfo( appId, out ignored );
            }

            stopWatch.Stop();

            Log.WriteInfo( "SteamAppInfo", "Loaded app name cache in {0}", stopWatch.Elapsed );
        }

        public bool GetDepotManifest( uint depotId, uint appId, out ulong manifest, string branch = "public" )
        {
            manifest = default( ulong );

            KeyValue appInfo;
            if ( !GetAppInfo( appId, out appInfo ) )
                return false;

            manifest = ( ulong )appInfo[ "depots" ][ depotId.ToString() ][ "manifests" ][ branch ].AsLong();

            return manifest != default( ulong );
        }

        public bool GetDepotName( uint depotId, uint appId, out string name )
        {
            name = null;

            KeyValue appInfo;
            if ( !GetAppInfo( appId, out appInfo ) )
                return false;

            name = appInfo[ "depots" ][ depotId.ToString() ][ "name" ].AsString();

            return name != null;
        }

        public bool GetPackageInfo( uint packageId, out KeyValue packageInfo )
        {
            packageInfo = KeyValue.LoadAsBinary( GetPackageCachePath( packageId ) );

            if ( packageInfo == null )
                return false;

            packageInfo = packageInfo.Children
                .FirstOrDefault();

            return packageInfo != null;
        }

        public bool GetPackageName( uint packageId, out string name )
        {
            name = null;

            KeyValue packageInfo;
            if ( !GetPackageInfo( packageId, out packageInfo ) )
                return false;

            name = packageInfo[ "name" ].AsString();

            return name != null;
        }


        public override void HandleMsg( IPacketMsg packetMsg )
        {
            switch ( packetMsg.MsgType )
            {
                case EMsg.ClientPICSChangesSinceResponse:
                    HandleChangesResponse( packetMsg );
                    break;

                case EMsg.ClientPICSProductInfoResponse:
                    HandleProductInfoResponse( packetMsg );
                    break;
            }
        }


        void HandleChangesResponse( IPacketMsg packetMsg )
        {
            var changesResponse = new ClientMsgProtobuf<CMsgClientPICSChangesSinceResponse>( packetMsg );

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
            var productInfo = new ClientMsgProtobuf<CMsgClientPICSProductInfoResponse>( packetMsg );

            foreach ( var app in productInfo.Body.apps )
            {
                string cacheFile = GetAppCachePath( app.appid );

                // appinfo contains a trailing null which we want to ignore
                var realBuffer = app.buffer
                    .Take( app.buffer.Length - 1 )
                    .ToArray();

                try
                {
                    File.WriteAllBytes( cacheFile, realBuffer );
                }
                catch ( IOException ex )
                {
                    Log.WriteError( "SteamAppInfoHandler", "Unable to cache appinfo for appid {0}: {1}", app.appid, ex.Message );
                    continue;
                }

                // force an app name cache
                KeyValue ignored;
                GetAppInfo( app.appid, out ignored );
            }

            foreach ( var package in productInfo.Body.packages )
            {
                string cacheFile = GetPackageCachePath( package.packageid );

                // packageinfo contains an integer before the actual contents
                var realBuffer = package.buffer
                    .Skip( 4 )
                    .ToArray();

                try
                {
                    File.WriteAllBytes( cacheFile, realBuffer );
                }
                catch ( IOException ex )
                {
                    Log.WriteError( "SteamAppInfoHandler", "Unable to cache packageinfo for packageid {0}: {1}", package.packageid, ex.Message );
                }
            }
        }


        static string GetAppCachePath( uint appId )
        {
            return Path.Combine( "cache", "appinfo", string.Format( "{0}.txt", appId ) );
        }

        static string GetPackageCachePath( uint packageId )
        {
            return Path.Combine( "cache", "packageinfo", string.Format( "{0}.bin", packageId ) );
        }
    }
}
