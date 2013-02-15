using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamIrcBot
{
    class PICSJob : Job
    {
        uint lastChangeNumber = 0;


        public PICSJob( CallbackManager manager )
        {
            Period = TimeSpan.FromSeconds( 1 );

            new JobCallback<SteamApps.PICSChangesCallback>( OnPICSChanges, manager );
        }


        protected override void OnRun()
        {
            if ( !Steam.Instance.Connected )
                return;

            Steam.Instance.Apps.PICSGetChangesSince( lastChangeNumber, true, true );
        }


        void OnPICSChanges( SteamApps.PICSChangesCallback callback, JobID jobId )
        {
            if ( lastChangeNumber == callback.CurrentChangeNumber )
                return;

            lastChangeNumber = callback.CurrentChangeNumber;

            IRC.Instance.SendAnnounce( "Got PICS changelist {0} with info for {1} apps and {2} packages! (fullupdate? {3})",
                lastChangeNumber, callback.AppChanges.Count, callback.PackageChanges.Count, callback.RequiresFullUpdate );

            if ( callback.AppChanges.Count > 0 )
            {
                IRC.Instance.SendAnnounce( "PICS Apps: {0}", string.Join( ", ", callback.AppChanges.Values.Select( a =>
                {
                    if ( a.NeedsToken )
                        return string.Format( "{0} (needs token)", GetAppName( a.ID ) );

                    return GetAppName( a.ID );
                } ) ) );
            }

            if ( callback.PackageChanges.Count > 0 )
            {
                IRC.Instance.SendAnnounce( "PICS Packages: {0}", string.Join( ", ", callback.PackageChanges.Values.Select( p =>
                {
                    if ( p.NeedsToken )
                        return string.Format( "{0} (needs token)", GetPackageName( p.ID ) );

                    return GetPackageName( p.ID );
                } ) ) );
            }
        }

        string GetAppName( uint appId )
        {
            string appName;

            if ( !Steam.Instance.AppInfo.GetAppName( appId, out appName ) )
                return appId.ToString();

            return string.Format( "{0} ({1})", appName, appId );
        }

        string GetPackageName( uint packageId )
        {
            string packageName;

            if ( !Steam.Instance.AppInfo.GetPackageName( packageId, out packageName ) )
                return packageId.ToString();

            return string.Format( "{0} ({1})", packageName, packageId );
        }
    }

}
