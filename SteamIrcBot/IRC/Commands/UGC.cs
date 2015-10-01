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

            Steam.Instance.CallbackManager.Subscribe<SteamCloud.UGCDetailsCallback>( OnUGCInfo );
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


        void OnUGCInfo( SteamCloud.UGCDetailsCallback callback )
        {
            var req = GetRequest( r => r.JobID == callback.JobID );

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

        UGCHandler ugcHandler;


        public PubFileCommand()
        {
            Triggers.Add( "!pubfile" );
            Triggers.Add( "!publishedfile" );
            HelpText = "!pubfile <pubfileid> - Requests published file details for the given published file ID";

            ugcHandler = Steam.Instance.SteamManager.GetHandler<UGCHandler>();
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

            var jobId = ugcHandler.RequestUGC( pubFileId, OnUGC );
            AddRequest( details, new Request { JobID = jobId, PubFileID = pubFileId } );
        }

        void OnUGC( UGCHandler.UGCJobResult result )
        {
            var req = GetRequest( r => r.JobID == result.ID );

            if ( req == null )
                return;

            if ( result.TimedOut )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request published file info: request timed out", req.Requester.Nickname );
                return;
            }

            if ( result.Result != EResult.OK )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request published file info: {1}", req.Requester.Nickname, result.Result );
                return;
            }

            PublishedFileDetails details = result.Details;

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

            displayDict.Add( "Desc.", details.short_description, true );
            displayDict.Add( "Creation", Utils.DateTimeFromUnixTime( details.time_created ) );
            displayDict.Add( "Vis.", ( EPublishedFileVisibility )details.visibility );
            displayDict.Add( "File", details.file_url );

            if ( details.banned )
            {
                displayDict.Add( "Ban Reason", details.ban_reason, true );
                displayDict.Add( "Banner", details.banner );
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
                displayDict.Add( "Score", string.Format( "{0:N4}", details.vote_data.score ) );
            }

            if ( details.for_sale_data != null )
            {
                if ( details.for_sale_data.is_for_sale )
                {
                    displayDict.Add( "For Sale", "True" );
                }

                displayDict.Add( "Sale Status", details.for_sale_data.estatus );
            }

            if ( details.kvtags.Count > 0 )
            {
                string kvTagsString = string.Join( ", ", details.kvtags.Select( kv => string.Format( "{0}={1}", kv.key, kv.value ) ) );
                displayDict.Add( "KVTags", kvTagsString );
            }

            if ( details.tags.Count > 0 )
            {
                string tagString = string.Join( ", ", details.tags.Select( tag =>
                {
                    if ( tag.adminonly )
                        return string.Format( "{0} (admin)", tag.tag );

                    return tag.tag;
                } ) );

                displayDict.Add( "Tags", tagString );
            }

            IRC.Instance.Send( req.Channel, "{0}: {1}: {2}", req.Requester.Nickname, req.PubFileID, displayDict );
        }
    }
}
