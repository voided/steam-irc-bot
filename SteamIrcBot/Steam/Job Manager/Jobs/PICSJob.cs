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
            Period = TimeSpan.FromSeconds( 5 );

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
                return; // no new changes

            lastChangeNumber = callback.CurrentChangeNumber;

            // group apps and package changes by changelist, this will seperate into individual changelists
            var appGrouping = callback.AppChanges
                .GroupBy( a => a.Value.ChangeNumber );

            var packageGrouping = callback.PackageChanges
                .GroupBy( p => p.Value.ChangeNumber );

            // join apps and packages back together based on changelist number
            var changeLists = appGrouping
                .Join( packageGrouping, a => a.Key, p => p.Key, ( a, p ) => new
                {
                    ChangeNumber = a.Key,

                    Apps = a.Select( app => app.Value ).ToList(),
                    Packages = p.Select( package => package.Value ).ToList(),
                } )
                .OrderBy( c => c.ChangeNumber );

            // the number of changes required in a changelist in order to be important enough to display
            // to all broadcast channels
            const int ChangesReqToBeImportant = 50;

            foreach ( var changeList in changeLists )
            {
                int numAppChanges = changeList.Apps.Count;
                int numPackageChanges = changeList.Packages.Count;

                string message = string.Format( "Got PICS changelist {0} for {1} apps and {2} packages - {3}",
                    changeList.ChangeNumber, changeList.Apps.Count, changeList.Packages.Count, GetChangelistURL( changeList.ChangeNumber ) );

                if ( numPackageChanges >= ChangesReqToBeImportant || numAppChanges >= ChangesReqToBeImportant )
                {
                    // if this changelist contains a number of changes over a specific threshold, we'll consider it "important" and send to all channels
                    IRC.Instance.SendAll( message );
                }
                else
                {
                    // otherwise, only send to announce
                    IRC.Instance.SendAnnounce( message );
                }

                if ( numAppChanges > 0 )
                {
                    // prioritize important apps first
                    var importantApps = changeList.Apps.Select( a => a.ID )
                        .Intersect( Settings.Current.ImportantApps );

                    foreach ( var app in importantApps )
                    {
                        IRC.Instance.SendAll( "Important App Update: {0} - {1}", Steam.Instance.GetAppName( app ), GetAppHistoryUrl( app ) );
                    }

                    // then announce all apps that changed
                    foreach ( var app in changeList.Apps )
                    {
                        IRC.Instance.SendAnnounce( "App: {0} {1}- {2}",
                            Steam.Instance.GetAppName( app.ID ),
                            app.NeedsToken ? "(needs token) " : "",
                            GetAppHistoryUrl( app.ID )
                        );
                    }
                }

                if ( numPackageChanges > 0 )
                {
                    // todo: important packages

                    foreach ( var package in changeList.Packages )
                    {
                        IRC.Instance.SendAnnounce( "Package: {0} {1}- {2}",
                            Steam.Instance.GetPackageName( package.ID ),
                            package.NeedsToken ? "(needs token) " : "",
                            GetPackageHistoryUrl( package.ID )
                        );
                    }
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
