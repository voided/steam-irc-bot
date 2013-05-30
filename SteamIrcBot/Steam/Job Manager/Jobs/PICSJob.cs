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

            string changelistUrl = string.Format( "http://steamdb.info/changelist.php?changeid={0}", lastChangeNumber );

            IRC.Instance.SendAnnounce( "Got PICS changelist {0} for {1} apps and {2} packages - {3}",
                lastChangeNumber, callback.AppChanges.Count, callback.PackageChanges.Count, changelistUrl );

            if ( callback.AppChanges.Count > 0 )
            {
                IRC.Instance.SendAnnounce( "PICS Apps: {0}", string.Join( ", ", callback.AppChanges.Values.Select( a =>
                {
                    if ( a.NeedsToken )
                        return string.Format( "{0} (needs token)", Steam.Instance.GetAppName( a.ID ) );

                    return Steam.Instance.GetAppName( a.ID );
                } ) ) );

                var importantApps = callback.AppChanges.Keys
                    .Intersect( Settings.Current.ImportantApps );

                foreach ( var app in importantApps )
                {
                    string historyUrl = string.Format( "http://steamdb.info/app/{0}/#section_history", app );
                    IRC.Instance.SendAll( "Important App Update: {0} - {1}", Steam.Instance.GetAppName( app ), historyUrl );
                }

            }

            if ( callback.PackageChanges.Count > 0 )
            {
                IRC.Instance.SendAnnounce( "PICS Packages: {0}", string.Join( ", ", callback.PackageChanges.Values.Select( p =>
                {
                    if ( p.NeedsToken )
                        return string.Format( "{0} (needs token)", Steam.Instance.GetPackageName( p.ID ) );

                    return Steam.Instance.GetPackageName( p.ID );
                } ) ) );
            }
        }
    }

}
