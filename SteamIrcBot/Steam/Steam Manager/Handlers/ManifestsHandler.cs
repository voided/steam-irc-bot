using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using DiffMatchPatch;
using SteamKit2;

namespace SteamIrcBot
{
/*
    class ManifestsHandler : SteamHandler
    {
        public ManifestsHandler( CallbackManager manager )
            : base( manager )
        {
            try
            {
                Directory.CreateDirectory( Path.Combine( "cache", "manifests" ) );
                Directory.CreateDirectory( Path.Combine( "cache", "manifest_diffs" ) );
            }
            catch ( IOException ex )
            {
                Log.WriteError( "Unable to create manifests/diffs cache directories: {0}", ex.Message );
            }

            new JobCallback<SteamApps.PICSProductInfoCallback>( OnProductInfo, manager );
        }


        async void OnProductInfo( SteamApps.PICSProductInfoCallback callback, JobID jobId )
        {
            var csServers = Steam.Instance.Client.GetServersOfType( EServerType.CS );
            var selectedCsServer = csServers[ new Random().Next( csServers.Count ) ];

            List<CDNClient.ClientEndPoint> cdnServers = null;

            for ( int attempt = 0 ; attempt < 5 ; ++attempt )
            {
                cdnServers = CDNClient.FetchServerList( new CDNClient.ClientEndPoint( selectedCsServer.Address.ToString(), selectedCsServer.Port ), ( int )Steam.Instance.CellID );

                if ( cdnServers != null )
                    break;
            }

            if ( cdnServers == null )
            {
                Log.WriteWarn( "ManifestsHandler", "Unable to get CDN server list" );
                return;
            }

            // filter down to content servers
            cdnServers = cdnServers
                .Where( ep => ep.Type == "CS" )
                .ToList();

            // flatten each apps depot list into one depot list
            var depots = callback.Apps.Values
                .SelectMany( app =>
                {
                    return app.KeyValues[ "depots" ].Children
                        .Select( depot => new
                        {
                            AppID = app.ID,
                            Depot = depot,
                        } );
                } );

            foreach ( var depotInfo in depots )
            {
                string[] ignoredKeys = { "overridescddb", "markdlcdepots", "branches", "preloadonly", "baselanguages" };

                if ( ignoredKeys.Any( k => k.Equals( depotInfo.Depot.Name, StringComparison.OrdinalIgnoreCase ) ) )
                    continue; // we ignore certain subkeys within the "depots" node

                uint depotId;

                if ( !uint.TryParse( depotInfo.Depot.Name, out depotId ) )
                {
                    Log.WriteWarn( "ManifestsHandler", "Unable to parse depot ID {0} for app {1}",
                        depotInfo.Depot.Name, Steam.Instance.GetAppName( depotInfo.AppID )
                    );
                    continue;
                }

                ulong manifest = ( ulong )depotInfo.Depot[ "manifests" ][ "public" ].AsLong();

                if ( manifest == default( ulong ) )
                {
                    // some depots don't have public manifests
                    // either because they're encrypted or the app configuration is invalid
                    continue;
                }

                ulong cachedManifest;
                if ( !GetRecentManifest( depotId, out cachedManifest ) )
                {
                    // no manifest cached, this is the first time we've seen this app
                    cachedManifest = 0;
                }

                if ( cachedManifest == manifest )
                    continue; // no changes

                var appTicket = await Steam.Instance.TaskManager.WaitForJob<SteamApps.AppOwnershipTicketCallback>( Steam.Instance.Apps.GetAppOwnershipTicket( depotId ) );

                if ( appTicket == null )
                {
                    Log.WriteWarn( "ManifestsHandler", "App ticket request for {0} timed out", Steam.Instance.GetDepotName( depotId, depotInfo.AppID ) );
                    continue;
                }

                if ( appTicket.Result != EResult.OK )
                {
                    if ( appTicket.Result == EResult.AccessDenied )
                        continue; // no license, so we ignore this depot

                    Log.WriteWarn( "ManifestsHandler", "Unable to request app ticket for {0}: {1}",
                        Steam.Instance.GetDepotName( depotId, depotInfo.AppID ), appTicket.Result
                    );

                    continue;
                }
                var selectedCdnServer = cdnServers[ new Random().Next( cdnServers.Count ) ];

                CDNClient cdnClient = new CDNClient( selectedCdnServer, appTicket.Ticket );

                if ( !cdnClient.Connect() )
                {
                    Log.WriteWarn( "ManifestsHandler", "Unable to connect to {0}:{1} for {2}",
                        selectedCdnServer.Host, selectedCdnServer.Port, Steam.Instance.GetDepotName( depotId, depotInfo.AppID )
                    );
                    continue;
                }

                var depotManifest = cdnClient.DownloadDepotManifest( ( int )depotId, manifest );
                if ( depotManifest == null )
                {
                    Log.WriteWarn( "ManifestsHandler", "Unable to download depot manifest for {0} / {1}",
                        Steam.Instance.GetDepotName( depotId, depotInfo.AppID ), manifest
                    );
                    continue;
                }

                if ( depotManifest.FilenamesEncrypted )
                {
                    var depotKey = await Steam.Instance.TaskManager.WaitForJob<SteamApps.DepotKeyCallback>( Steam.Instance.Apps.GetDepotDecryptionKey( depotId ) );

                    if ( depotKey == null )
                    {
                        Log.WriteWarn( "ManifestsHandler", "Depot manifest for {0} / {1} is encrypted and the depot key request timed out",
                            Steam.Instance.GetDepotName( depotId, depotInfo.AppID ), manifest
                        );
                        continue;
                    }

                    if ( depotKey.Result != EResult.OK )
                    {
                        if ( depotKey.Result == EResult.Blocked )
                            continue; // no license for depot

                        Log.WriteWarn( "ManifestsHandler", "Depot manifest for {0} / {1} is encrypted and the depot key request failed with: {2}",
                            Steam.Instance.GetDepotName( depotId, depotInfo.AppID ), manifest, depotKey.Result
                        );

                        continue;
                    }

                    depotManifest.DecryptFilenames( depotKey.DepotKey );
                }

                string oldManifestFile = GetManifestCachePath( depotId, cachedManifest );
                string oldManifest = "";

                if ( File.Exists( oldManifestFile ) )
                {
                    oldManifest = File.ReadAllText( oldManifestFile );
                }

                string newManifest = ManifestToString( depotManifest );

                // write out the new manifest to cache
                File.WriteAllText( GetManifestCachePath( depotId, manifest ), newManifest );

                if ( !Settings.Current.IsWebEnabled )
                {
                    Log.WriteWarn( "ManifestsHandler", "Unable to write out manifest for {0}: Web support is disabled",
                        Steam.Instance.GetDepotName( depotId, depotInfo.AppID ) 
                    );
                    continue;
                }

                diff_match_patch diff = new diff_match_patch();
                diff.Diff_Timeout = 0;

                var diffList = diff.diff_main( oldManifest, newManifest, false );
                diff.diff_cleanupSemantic( diffList );

                string diffString = DiffsToHtml( diffList, depotInfo.AppID, depotId, cachedManifest, manifest );

                // write out diff
                File.WriteAllText( GetManifestDiffPath( depotId, cachedManifest, manifest ), diffString );

                // the manifest diff has changes if at least one of the diffs in the list isn't equal
                bool hasDiffs = diffList
                    .Any( d => d.operation != Operation.EQUAL );

                if ( Settings.Current.ImportantApps.Contains( depotInfo.AppID ) && hasDiffs )
                {
                    string path = Path.Combine( "manifest_diffs", depotId.ToString(), string.Format( "{0}_{1}.html", cachedManifest, manifest ) );
                    var webUri = new Uri( new Uri( Settings.Current.WebURL ), path );

                    IRC.Instance.SendAll( "Important depot change for {0}: {1}",
                        Steam.Instance.GetDepotName( depotId, depotInfo.AppID ), webUri
                    );
                }
            }
        }


        string ManifestToString( DepotManifest manifest )
        {
            StringBuilder sb = new StringBuilder();

            // manifests contents seem to be sorted in a randomish format
            var sortedFiles = manifest.Files
                .OrderBy( f => f.FileName, StringComparer.OrdinalIgnoreCase );

            foreach ( var file in sortedFiles )
            {
                EDepotFileFlag flags = file.Flags;

                string cleanFileName = file.FileName;

                if ( manifest.FilenamesEncrypted )
                {
                    // valve is following some base64 spec where newlines need to be inserted after 72 characters of a base64 string
                    // so we'll lazily remove all newline chars from the filename
                    cleanFileName = cleanFileName.Replace( "\n", "" );
                }

                if ( flags.HasFlag( EDepotFileFlag.Directory ) )
                {
                    // directories don't have a useful size value
                    sb.AppendFormat( "{0} Flags: {1:G}", cleanFileName, flags );
                    sb.AppendLine();
                }
                else
                {
                    string flagsString = "";

                    if ( flags != 0 )
                        flagsString = string.Format( "Flags: {0:G}", flags );

                    sb.AppendFormat( "{0} {1} {2}", cleanFileName, PrettySize( ( long )file.TotalSize ), flagsString );
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        string PrettySize( long byteCount )
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB

            if ( byteCount == 0 )
                return "0B";

            long bytes = Math.Abs( byteCount );
            int place = Convert.ToInt32( Math.Floor( Math.Log( bytes, 1024 ) ) );
            double num = Math.Round( bytes / Math.Pow( 1024, place ), 1 );

            return string.Format( "{0}{1}", Math.Sign( byteCount ) * num, suf[ place ] );
        }

        static string DiffsToHtml( List<Diff> diffs, uint appId, uint depotId, ulong oldManifest, ulong newManifest )
        {
            string depot = Steam.Instance.GetDepotName( depotId, appId );

            string title = string.Format( "{0} - {1} vs {2}", depot, oldManifest, newManifest );

            var sb = new StringBuilder();

            sb.AppendFormat( @"
                <!DOCTYPE html>
                <html>
                    <head>
                        <title>{0}</title>
                        <meta charset=""utf-8""> 
                        <style>
                            body
                            {{
                                font-family: Consolas, ""Liberation Mono"", Courier, monospace;
                            }}
                            #info
                            {{
                                font-size: 120%;
                                padding-top: 20px;
                                padding-bottom: 20px;
                            }}
                            .ins
                            {{
                                background: #e6ffe6;
                            }}
                            .del
                            {{
                                background: #ffe6e6;
                            }}
                        </style>
                    </head>
                    <body>
                    ", title );

            sb.AppendFormat( @"
                        <div id=""info"">
                            Depot: {0}<br />
                            Diff: {1} to {2}
                        </div>
                        ", depot, oldManifest, newManifest );

            foreach ( var diff in diffs )
            {
                string text = HttpUtility.HtmlEncode( diff.text );
                text = text.Replace( "\n", "<br />" );

                switch ( diff.operation )
                {
                    case Operation.EQUAL:
                        sb.AppendFormat( "<span>{0}</span>", text );
                        break;

                    case Operation.INSERT:
                        sb.AppendFormat( @"<span class=""ins"">{0}</span>", text );
                        break;

                    case Operation.DELETE:
                        sb.AppendFormat( @"<span class=""del"">{0}</span>", text );
                        break;
                }
            }

            sb.Append( @"
                    </body>
                </html>" );

            return sb.ToString();
        }

        string GetManifestCachePath( uint depotId, ulong manifest )
        {
            string manifestPath = Path.Combine( "cache", "manifests", depotId.ToString() );

            if ( !Directory.Exists( manifestPath ) )
            {
                try
                {
                    Directory.CreateDirectory( manifestPath );
                }
                catch ( Exception ex )
                {
                    Log.WriteWarn( "ManifestsHandler", "Unable to create manifest cache directory: {0}", ex.Message );
                    return null;
                }
            }

            return Path.Combine( manifestPath, string.Format( "{0}.txt", manifest ) );
        }
        string GetManifestDiffPath( uint depotId, ulong oldManifest, ulong newManifest )
        {
            string diffPath = Path.Combine( Settings.Current.WebPath, "manifest_diffs", depotId.ToString() );

            if ( !Directory.Exists( diffPath ) )
            {
                try
                {
                    Directory.CreateDirectory( diffPath );
                }
                catch ( Exception ex )
                {
                    Log.WriteWarn( "ManifestsHandler", "Unable to create manifest diff cache directory: {0}", ex.Message );
                    return null;
                }
            }

            return Path.Combine( diffPath, string.Format( "{0}_{1}.html", oldManifest, newManifest ) );
        }

        bool GetRecentManifest( uint depotId, out ulong manifestId )
        {
            manifestId = 0;

            string manifestPath = Path.Combine( "cache", "manifests", depotId.ToString() );

            if ( !Directory.Exists( manifestPath ) )
            {
                try
                {
                    Directory.CreateDirectory( manifestPath );
                }
                catch ( Exception ex )
                {
                    Log.WriteWarn( "ManifestsHandler", "Unable to create manifest cache directory: {0}", ex.Message );
                    return false;
                }
            }

            var dirInfo = new DirectoryInfo( manifestPath );

            var recentFile = dirInfo.GetFiles( "*.txt" )
                .OrderByDescending( f => f.LastWriteTime )
                .FirstOrDefault();

            if ( recentFile == null )
                return false;

            // strip .txt
            string manifestName = recentFile.Name.Substring( 0, recentFile.Name.Length - 4 );

            return ulong.TryParse( manifestName, out manifestId );
        }
    }
*/

}
