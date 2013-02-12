using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamIrcBot
{
    class PIDCommand : Command<PIDCommand.Request>
    {
        public class Request : BaseRequest
        {
            public JobID JobID { get; set; }

            public uint PackageID { get; set; }
        }

        public PIDCommand()
        {
            Trigger = "!pid";

            new JobCallback<SteamApps.PackageInfoCallback>( OnPackageInfo, Steam.Instance.CallbackManager );
        }

        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: PackageID argument required", details.Sender.Nickname );
                return;
            }

            uint packageId;
            if ( !uint.TryParse( details.Args[ 0 ], out packageId ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid PackageID", details.Sender.Nickname );
                return;
            }

            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, details.Sender.Nickname + ": Unable to request package name: not connected to Steam!" );
                return;
            }

            var jobId = Steam.Instance.Apps.GetPackageInfo( packageId );
            AddRequest( details, new Request { JobID = jobId, PackageID = packageId } );
        }


        void OnPackageInfo( SteamApps.PackageInfoCallback callback, JobID jobId )
        {
            var req = GetRequest( r => r.JobID == jobId );

            if ( req == null )
                return;

            var package = callback.Packages
                .FirstOrDefault( p => p.PackageID == req.PackageID );

            if ( package == null || package.Status == SteamApps.PackageInfoCallback.Package.PackageStatus.Unknown )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request package name for {1}: unknown PackageID", req.Requester.Nickname, req.PackageID );
                return;
            }

            var name = package.Data[ "name" ].AsString();

            if ( string.IsNullOrEmpty( name ) )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request package name for {1}: no name assigned", req.Requester.Nickname, req.PackageID );
                return;
            }

            IRC.Instance.Send( req.Channel, "{0}: {1}: {2} (http://store.steampowered.com/sub/{1})", req.Requester.Nickname, req.PackageID, name );
        }
    }
}
