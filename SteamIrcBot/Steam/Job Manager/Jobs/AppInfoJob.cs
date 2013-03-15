using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamIrcBot
{
#if false
    class AppInfoJob : Job
    {
        uint lastChangeNumber = 0;


        public AppInfoJob( CallbackManager manager )
        {
            Period = TimeSpan.FromSeconds( 1 );

            new Callback<SteamApps.AppChangesCallback>( OnAppChanges, manager );
        }


        protected override void OnRun()
        {
            if ( !Steam.Instance.Connected )
                return;

            Steam.Instance.Apps.GetAppChanges( lastChangeNumber );
        }


        void OnAppChanges( SteamApps.AppChangesCallback callback )
        {
            if ( lastChangeNumber == callback.CurrentChangeNumber )
                return;

            lastChangeNumber = callback.CurrentChangeNumber;

            IRC.Instance.SendAnnounce( "Got AppInfo changelist {0} with info for {1} apps! (fullupdate? {2})",
                lastChangeNumber, callback.AppIDs.Count, callback.ForceFullUpdate );

            if ( callback.AppIDs.Count > 0 )
            {
                IRC.Instance.SendAnnounce( "AppInfo Apps: {0}", string.Join( ", ", callback.AppIDs ) );
            }
        }
    }
#endif
}