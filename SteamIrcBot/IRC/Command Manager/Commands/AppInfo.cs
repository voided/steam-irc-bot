using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamIrcBot
{
    class GIDCommand : Command<GIDCommand.Request>
    {
        public class Request : BaseRequest
        {
            public JobID JobID { get; set; }

            public uint AppID { get; set; }
        }


        public GIDCommand()
        {
            Trigger = "!gid";
            HelpText = "!gid <appid> - Requests app name for given AppID";

            new JobCallback<SteamApps.AppInfoCallback>( OnAppInfo, Steam.Instance.CallbackManager );
        }


        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: AppID argument required", details.Sender.Nickname );
                return;
            }

            uint appId;
            if ( !uint.TryParse( details.Args[ 0 ], out appId ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid AppID", details.Sender.Nickname );
                return;
            }

            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, details.Sender.Nickname + ": Unable to request app name: not connected to Steam!" );
                return;
            }

            // send off a product request as well so we get something to cache for later
            Steam.Instance.Apps.PICSGetProductInfo( appId, null, false, false );

            var jobId = Steam.Instance.Apps.GetAppInfo( appId );
            AddRequest( details, new Request { JobID = jobId, AppID = appId } );
        }


        void OnAppInfo( SteamApps.AppInfoCallback callback, JobID jobId )
        {
            var req = GetRequest( r => r.JobID == jobId );

            if ( req == null )
                return;

            var app = callback.Apps
                .FirstOrDefault( a => a.AppID == req.AppID );

            if ( app == null || app.Status == SteamApps.AppInfoCallback.App.AppInfoStatus.Unknown )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request app name for {1}: unknown AppID", req.Requester.Nickname, req.AppID );
                return;
            }

            KeyValue commonSection;
            if ( !app.Sections.TryGetValue( EAppInfoSection.Common, out commonSection ) )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request app name for {1}: no common section", req.Requester.Nickname, req.AppID );
                return;
            }

            var name = commonSection[ "name" ].AsString();

            if ( string.IsNullOrEmpty( name ) )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request app name for {1}: no name assigned", req.Requester.Nickname, req.AppID );
                return;
            }

            IRC.Instance.Send( req.Channel, "{0}: {1}: {2} (http://store.steampowered.com/app/{1} / http://steamcommunity.com/app/{1})", req.Requester.Nickname, req.AppID, name );
        }
    }
}
