using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamIrcBot
{
    class UGCCommand : Command<UGCCommand.Request>
    {
        public class Request : BaseRequest
        {
            public JobID JobID { get; set; }

            public UGCHandle UGC { get; set; }
        }


        public UGCCommand()
        {
            Trigger = "!ugc";
            HelpText = "!ugc <ugcid> - Requests UGC details for the given UGC ID";

            new JobCallback<SteamCloud.UGCDetailsCallback>( OnUGCInfo, Steam.Instance.CallbackManager );
        }


        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: UGC ID argument required", details.Sender.Nickname );
                return;
            }

            ulong ugcId;
            if ( !ulong.TryParse( details.Args[ 0 ], out ugcId ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid UGC ID", details.Sender.Nickname );
                return;
            }

            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to request UGC info: not connected to Steam!", details.Sender.Nickname );
                return;
            }

            var jobId = Steam.Instance.Cloud.RequestUGCDetails( ugcId );
            AddRequest( details, new Request { JobID = jobId, UGC = ugcId } );
        }


        void OnUGCInfo( SteamCloud.UGCDetailsCallback callback, JobID jobId )
        {
            var req = GetRequest( r => r.JobID == jobId );

            if ( req == null )
                return;

            if ( callback.Result != EResult.OK )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request UGC info: {1}", req.Requester.Nickname, callback.Result );
                return;
            }

            var displayDict = new DisplayDictionary();

            displayDict.Add( "URL", callback.URL );
            displayDict.Add( "Creator", callback.Creator );
            displayDict.Add( "App", callback.AppID );
            displayDict.Add( "File", string.Format( "\"{0}\"", callback.FileName ) );
            displayDict.Add( "Size", Utils.GetByteSizeString( callback.FileSize ) );

            IRC.Instance.Send( req.Channel, "{0}: {1}: {2}", req.Requester.Nickname, req.UGC, displayDict );
        }
    }
}
