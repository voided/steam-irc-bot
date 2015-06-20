using System;
using System.Collections.Generic;
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
        public class UGCJob
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


        Dictionary<JobID, UGCJob> ugcJobs = new Dictionary<JobID, UGCJob>();


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

            new Callback<SteamUnifiedMessages.ServiceMethodResponse>( OnServiceMethod, manager );
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
            PublishedFileDetails fileDetails = GetDetailsFromCache( pubFile );

            if ( fileDetails == null )
            {
                name = null;
                return false;
            }

            name = fileDetails.title;
            return true;
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

        PublishedFileDetails GetDetailsFromCache( ulong pubFile )
        {
            string fileName = GetCachePath( pubFile );

            if ( !File.Exists( fileName ) )
            {
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

            // cache out this ugc to file

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
