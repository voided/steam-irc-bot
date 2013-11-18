using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;
using SteamKit2.Unified.Internal;

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
            Triggers.Add( "!ugc" );
            Triggers.Add( "!ugcid" );
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
            displayDict.Add( "File", callback.FileName, true );
            displayDict.Add( "Size", Utils.GetByteSizeString( callback.FileSize ) );

            IRC.Instance.Send( req.Channel, "{0}: {1}: {2}", req.Requester.Nickname, req.UGC, displayDict );
        }
    }

    class PubFileCommand : Command<PubFileCommand.Request>
    {
        public class Request : BaseRequest
        {
            public JobID JobID { get; set; }

            public PublishedFileID PubFileID { get; set; }
        }


        public PubFileCommand()
        {
            Triggers.Add( "!pubfile" );
            Triggers.Add( "!publishedfile" );
            HelpText = "!pubfile <pubfileid> - Requests published file details for the given published file ID";

            new JobCallback<SteamUnifiedMessages.ServiceMethodResponse>( OnServiceMethod, Steam.Instance.CallbackManager );
        }


        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: Published File ID argument required", details.Sender.Nickname );
                return;
            }

            ulong pubFileId;
            if ( !ulong.TryParse( details.Args[ 0 ], out pubFileId ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid Published File ID", details.Sender.Nickname );
                return;
            }

            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to request published file info: not connected to Steam!", details.Sender.Nickname );
                return;
            }

            var pubFileRequest = new CPublishedFile_GetDetails_Request();
            pubFileRequest.publishedfileids.Add( pubFileId );

            var jobId = Steam.Instance.PublishedFiles.SendMessage( api => api.GetDetails( pubFileRequest ) );
            AddRequest( details, new Request { JobID = jobId, PubFileID = pubFileId } );
        }

        void OnServiceMethod( SteamUnifiedMessages.ServiceMethodResponse callback, JobID jobId )
        {
            var req = GetRequest( r => r.JobID == jobId );

            if ( req == null )
                return;

            if ( callback.Result != EResult.OK )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to make service request for published file info: {1}", req.Requester.Nickname, callback.Result );
                return;
            }

            var response = callback.GetDeserializedResponse<CPublishedFile_GetDetails_Response>();
            var details = response.publishedfiledetails.FirstOrDefault();

            if ( details == null )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request published file info: the server returned no info!", req.Requester.Nickname );
                return;
            }

            EResult result = ( EResult )details.result;

            if ( result != EResult.OK )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to get published file info: {1}", result );
                return;
            }

            var displayDict = new DisplayDictionary();

            displayDict.Add( "Title", details.title, true );
            displayDict.Add( "URL", details.url );
            displayDict.Add( "Creator", details.creator );
            displayDict.Add( "Creator App", Steam.Instance.GetAppName( details.creator_appid ) );
            displayDict.Add( "Consumer App", Steam.Instance.GetAppName( details.consumer_appid ) );

            if ( details.shortcutid != 0 )
            {
                displayDict.Add( "Creator Game", details.shortcutname );
                displayDict.Add( "Consumer Game", details.consumer_shortcutid );
            }

            if ( details.hcontent_file != 0 )
            {
                displayDict.Add( "File UGC", details.hcontent_file );
            }

            if ( details.hcontent_preview != 0 )
            {
                displayDict.Add( "Preview UGC", details.hcontent_preview );
            }

            displayDict.Add( "Description", details.short_description, true );
            displayDict.Add( "Creation Time", Utils.DateTimeFromUnixTime( details.time_created ) );
            displayDict.Add( "Visibility", ( EPublishedFileVisibility )details.visibility );
            displayDict.Add( "File", details.file_url );

            if ( details.banned )
            {
                displayDict.Add( "Ban Reason", details.ban_reason, true );
                displayDict.Add( "Banner", details.banned );
            }

            if ( details.incompatible )
            {
                displayDict.Add( "Incompatible", "True" );
            }

            if ( details.num_reports > 0 )
            {
                displayDict.Add( "# Reports", details.num_reports.ToString() );
            }

            if ( details.vote_data != null )
            {
                displayDict.Add( "Votes", string.Format( "{0} up, {1} down", details.vote_data.votes_up, details.vote_data.votes_down ) );
            }

            IRC.Instance.Send( req.Channel, "{0}: {1}: {2}", req.Requester.Nickname, req.PubFileID, displayDict );
        }
    }
}
