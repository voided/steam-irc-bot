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

            new JobCallback<SteamWorkshop.PublishedFileDetailsCallback>( OnPubInfo, Steam.Instance.CallbackManager );
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

            var jobId = Steam.Instance.Workshop.RequestPublishedFileDetails( pubFileId );
            AddRequest( details, new Request { JobID = jobId, PubFileID = pubFileId } );
        }


        void OnPubInfo( SteamWorkshop.PublishedFileDetailsCallback callback, JobID jobId )
        {
            var req = GetRequest( r => r.JobID == jobId );

            if ( req == null )
                return;

            if ( callback.Result != EResult.OK )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request published file info: {1}", req.Requester.Nickname, callback.Result );
                return;
            }

            var displayDict = new DisplayDictionary();

            displayDict.Add( "Title",  callback.Title, true );
            displayDict.Add( "URL", callback.URL );
            displayDict.Add( "Creator", callback.Creator );
            displayDict.Add( "Creator App", Steam.Instance.GetAppName( callback.CreatorAppID ) );
            displayDict.Add( "Consumer App", Steam.Instance.GetAppName( callback.ConsumerAppID ) );
            displayDict.Add( "File UGC", callback.FileUGC );
            displayDict.Add( "Preview UGC", callback.PreviewFileUGC );
            displayDict.Add( "Description", callback.Description, true );
            displayDict.Add( "Creation Time", callback.CreationTime );
            displayDict.Add( "Visibility", callback.Visiblity );
            displayDict.Add( "File", callback.FileName, true );

            IRC.Instance.Send( req.Channel, "{0}: {1}: {2}", req.Requester.Nickname, req.PubFileID, displayDict );

        }

    }
}
