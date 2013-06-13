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

            IRC.Instance.SendAnnounce( "Got PICS changelist {0} for {1} apps and {2} packages - {3}",
                lastChangeNumber, callback.AppChanges.Count, callback.PackageChanges.Count, GetChangelistURL( lastChangeNumber ) );

            if ( callback.AppChanges.Count > 0 )
            {
                // prioritize important apps first
                var importantApps = callback.AppChanges.Keys
                    .Intersect( Settings.Current.ImportantApps );

                foreach ( var app in importantApps )
                {
                    IRC.Instance.SendAll( "Important App Update: {0} - {1}", Steam.Instance.GetAppName( app ), GetAppHistoryUrl( app ) );
                }

                // then announce all apps that changed
                foreach ( var app in callback.AppChanges.Values )
                {
                    IRC.Instance.SendAnnounce( "App: {0} {1}- {2}",
                        Steam.Instance.GetAppName( app.ID ),
                        app.NeedsToken ? "(needs token) " : "",
                        GetAppHistoryUrl( app.ID )
                    );
                }
            }

            if ( callback.PackageChanges.Count > 0 )
            {
                // todo: important packages

                foreach ( var package in callback.PackageChanges.Values )
                {
                    IRC.Instance.SendAnnounce( "Package: {0} {1}- {2}",
                        Steam.Instance.GetPackageName( package.ID ),
                        package.NeedsToken ? "(needs token) " : "",
                        GetPackageHistoryUrl( package.ID )
                    );
                }
            }
        }

        string GetChangelistURL( uint changeNumber )
        {
            return GetSteamDBUrl( Settings.Current.SteamDBChangelistURL, changeNumber );
        }

        string GetAppHistoryUrl( uint appId )
        {
            return GetSteamDBUrl( Settings.Current.SteamDBAppHistoryURL, appId );
        }
        string GetPackageHistoryUrl( uint packageId )
        {
            return GetSteamDBUrl( Settings.Current.SteamDBPackageHistoryURL, packageId );
        }

        string GetSteamDBUrl( string formatUrl, uint id )
        {
            if ( !string.IsNullOrEmpty( formatUrl ) )
            {
                try
                {
                    formatUrl = string.Format( formatUrl, id );
                }
                catch ( FormatException ex )
                {
                    Log.WriteWarn( "Unable to format SteamDB url: {0}", ex.Message );
                }
            }

            return formatUrl;
        }
    }

}
