using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;
using SteamKit2;
using SteamKit2.Unified.Internal;

namespace SteamIrcBot
{
    class UGCHandler : SteamHandler
    {
        class UGCJob
        {
            public DateTime StartTime { get; set; }

            public Action<UGCJobResult> Callback { get; set; }

            public object UserData { get; set; }


            public UGCJob( Action<UGCJobResult> callback )
            {
                this.StartTime = DateTime.UtcNow;

                this.Callback = callback;
            }
        }

        public class UGCJobResult
        {
            public JobID ID { get; set; }

            public EResult Result { get; set; }

            public bool TimedOut { get; set; }

            public PublishedFileDetails Details { get; set; }


            public UGCJobResult()
            {
                this.Result = EResult.OK;
            }
            public UGCJobResult( JobID id )
                : this()
            {
                this.ID = id;
            }
            public UGCJobResult( JobID id, EResult result )
            {
                this.ID = id;
                this.Result = result;
            }
        }

        public class UGCCacheEntry
        {
            public ulong PubFileID { get; set; }

            public uint AppID { get; set; }

            public string Name { get; set; }

            public List<PublishedFileDetails.Tag> Tags { get; set; }
        }


        Dictionary<JobID, UGCJob> ugcJobs = new Dictionary<JobID, UGCJob>();

        Dictionary<ulong, UGCCacheEntry> ugcCache = new Dictionary<ulong, UGCCacheEntry>();
        bool loadedCache = false;


        public UGCHandler( CallbackManager manager )
            : base( manager )
        {
            try
            {
                Directory.CreateDirectory( Path.Combine( "cache", "ugc" ) );
            }
            catch ( IOException ex )
            {
                Log.WriteError( "UGCHandler", "Unable to create ugc cache directory: {0}", ex.Message );
            }

            manager.Subscribe<SteamUnifiedMessages.ServiceMethodResponse>( OnServiceMethod );

            Steam.Instance.CallbackManager.Subscribe<SteamUser.LoggedOnCallback>( OnLoggedOn );
        }


        public JobID RequestUGC( ulong pubFile, Action<UGCJobResult> callback, object userData = null )
        {
            var pubFileRequest = new CPublishedFile_GetDetails_Request
            {
                includetags = true,
                includekvtags = true,
                includevotes = true,
                includemetadata = true,
                includeforsaledata = true,
            };

            pubFileRequest.publishedfileids.Add( pubFile );

            JobID jobId = Steam.Instance.PublishedFiles.SendMessage( api => api.GetDetails( pubFileRequest ) );

            var job = new UGCJob( callback );
            job.UserData = userData;

            ugcJobs[ jobId ] = job;

            return jobId;
        }

        public bool LookupUGCName( ulong pubFile, out string name )
        {
            name = null;

            UGCCacheEntry cacheEntry;

            if ( ugcCache.TryGetValue( pubFile, out cacheEntry ) )
            {
                name = cacheEntry.Name;

                // we had the name cached in memory, no need to touch disk
                return true;
            }

            // otherwise, need to see if we have it on disk

            PublishedFileDetails fileDetails = GetDetailsFromDisk( pubFile );

            if ( fileDetails == null )
            {
                // couldn't load details from disk cache, lets try requesting info from steam
                RequestUGC( pubFile, res => { } );

                return false;
            }

            name = fileDetails.title;

            ugcCache[pubFile] = new UGCCacheEntry
            {
                PubFileID = pubFile,
                AppID = fileDetails.consumer_appid,

                Name = name,
                Tags = fileDetails.tags
            };

            return true;
        }

        public bool LookupUGC( ulong pubFile, out UGCCacheEntry entry )
        {
            entry = null;

            if ( ugcCache.TryGetValue( pubFile, out entry ) )
            {
                return true;
            }

            // nothing in our cache, ask steam
            RequestUGC( pubFile, res => { } );

            return false;
        }

        public bool FindUGC( string search, out ulong pubFileId )
        {
            pubFileId = 0;
            
            var ugcMatches = ugcCache
                .Where( kvp => kvp.Value.Name.IndexOf( search, StringComparison.OrdinalIgnoreCase ) != -1 )
                .ToList();

            if ( ugcMatches.Count == 0 )
            {
                return false;
            }

            var ugcList = ugcMatches.Select( kvp => kvp.Value );

            UGCCacheEntry searchResult = ugcList.FirstOrDefault();

            if ( searchResult != null )
            {
                pubFileId = searchResult.PubFileID;
                return true;
            }

            return false;
        }


        public override void Tick()
        {
            var jobsTimedOut = new List<Tuple<JobID, UGCJob>>();

            foreach ( var job in ugcJobs )
            {
                // figure out which jobs timed out

                TimeSpan jobTime = DateTime.UtcNow - job.Value.StartTime;

                if ( jobTime > TimeSpan.FromSeconds( 5 ) )
                {
                    jobsTimedOut.Add( Tuple.Create( job.Key, job.Value ) );
                }
            }
            
            foreach ( var job in jobsTimedOut )
            {
                // trigger callback, let our caller know we never got a response
                job.Item2.Callback( new UGCJobResult { ID = job.Item1, TimedOut = true } );

                // remove it from our list
                ugcJobs.Remove( job.Item1 );
            }
        }

        PublishedFileDetails GetDetailsFromDisk( ulong pubFile )
        {
            string fileName = GetCachePath( pubFile );

            if ( !File.Exists( fileName ) )
            {
                return null;
            }

            DateTime creationTime = File.GetLastWriteTimeUtc( fileName );

            if ( creationTime + TimeSpan.FromDays( 1 ) < DateTime.UtcNow )
            {
                // stale data, purge it and lets request new data
                File.Delete( fileName );

                return null;
            }

            byte[] fileData = null;

            try
            {
                fileData = File.ReadAllBytes( fileName );
            }
            catch ( IOException ex )
            {
                Log.WriteError( "UGCHandler", "Unable to load ugc {0} from cache: {1}", pubFile, ex.Message );
                return null;
            }

            using ( var ms = new MemoryStream( fileData ) )
            {
                return Serializer.Deserialize<PublishedFileDetails>( ms );
            }
        }

        void CacheUGC()
        {
            Stopwatch stopWatch = Stopwatch.StartNew();

            foreach ( string file in Directory.EnumerateFiles( Path.Combine( "cache", "ugc" ) ) )
            {
                string filePubFileId = Path.GetFileNameWithoutExtension( file );

                ulong pubFileId;

                if ( !ulong.TryParse( filePubFileId, out pubFileId ) )
                    return;

                string ignored;
                LookupUGCName( pubFileId, out ignored );
            }

            stopWatch.Stop();

            Log.WriteInfo( "UGCHandler", "Loaded ugc cache in {0}", stopWatch.Elapsed );
        }

        void OnLoggedOn( SteamUser.LoggedOnCallback callback )
        {
            if ( loadedCache )
                return;

            if ( callback.Result == EResult.OK )
            {
                // we've logged on, now we can cache our ugc so that we can request new ugc details for stale files

                loadedCache = true;

                CacheUGC();
            }
        }

        void OnServiceMethod( SteamUnifiedMessages.ServiceMethodResponse callback )
        {
            UGCJob ugcJob;
            bool foundJob = ugcJobs.TryGetValue( callback.JobID, out ugcJob );
            ugcJobs.Remove( callback.JobID );

            if ( !foundJob )
            {
                // didn't find a waiting UGC job for this response
                return;
            }

            if ( callback.Result != EResult.OK )
            {
                ugcJob.Callback( new UGCJobResult( callback.JobID, callback.Result ) );
                return;
            }

            var response = callback.GetDeserializedResponse<CPublishedFile_GetDetails_Response>();
            var details = response.publishedfiledetails.FirstOrDefault(); // we only ever request one pubfile at a time

            EResult pubResult = (EResult)details.result;

            if ( pubResult != EResult.OK )
            {
                ugcJob.Callback( new UGCJobResult ( callback.JobID, pubResult ) );
                return;
            }

            // cache out this ugc to file and memory

            ugcCache[details.publishedfileid] = new UGCCacheEntry
            {
                PubFileID = details.publishedfileid,
                AppID = details.consumer_appid,

                Name = details.title,
                Tags = details.tags
            };

            using ( var ms = new MemoryStream() )
            {
                Serializer.Serialize( ms, details );

                try
                {
                    File.WriteAllBytes( GetCachePath( details.publishedfileid ), ms.ToArray() );
                }
                catch ( IOException ex )
                {
                    Log.WriteError( "UGCHandler", "Unable to cache ugc for pubfile {0}: {1}", details.publishedfileid, ex.Message );
                    return;
                }
            }

            var result = new UGCJobResult
            {
                ID = callback.JobID,
                Details = details,
            };

            ugcJob.Callback( result );
        }


        static string GetCachePath( ulong pubFile )
        {
            return Path.Combine( "cache", "ugc", string.Format( "{0}.bin", pubFile ) );
        }
    }
}
