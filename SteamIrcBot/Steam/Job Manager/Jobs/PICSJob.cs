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

            new Callback<SteamApps.PICSChangesCallback>( OnPICSChanges, manager );
        }


        protected override void OnRun()
        {
            Steam.Instance.AppInfo.CacheRandomApp();

            if ( !Steam.Instance.Connected )
                return;

            Steam.Instance.Apps.PICSGetChangesSince( lastChangeNumber, true, true );
        }


        void OnPICSChanges( SteamApps.PICSChangesCallback callback )
        {
            // group apps and package changes by changelist, this will seperate into individual changelists
            var appGrouping = callback.AppChanges
                .Select( a => a.Value )
                .GroupBy( a => a.ChangeNumber );

            var packageGrouping = callback.PackageChanges
                .Select( p => p.Value )
                .GroupBy( p => p.ChangeNumber );

            // join apps and packages back together based on changelist number
            var changeLists = appGrouping
                .FullOuterJoin( packageGrouping, a => a.Key, p => p.Key, ( a, p, key ) => new
                {
                    ChangeNumber = key,

                    Apps = a.ToList(),
                    Packages = p.ToList(),
                },
                new EmptyGrouping<uint, SteamApps.PICSChangesCallback.PICSChangeData>(),
                new EmptyGrouping<uint, SteamApps.PICSChangesCallback.PICSChangeData>() )
                .OrderBy( c => c.ChangeNumber );

            // the number of changes required in a changelist in order to be important enough to display
            // to all broadcast channels
            const int ChangesReqToBeImportant = 50;

            if ( changeLists.Count() == 0 )
            {
                // this will happen on the first changes request
                lastChangeNumber = callback.CurrentChangeNumber;
                return;
            }

            foreach ( var changeList in changeLists )
            {
                if ( changeList.ChangeNumber <= lastChangeNumber )
                    return; // old changelist

                lastChangeNumber = changeList.ChangeNumber;

                int numAppChanges = changeList.Apps.Count;
                int numPackageChanges = changeList.Packages.Count;

                string message = string.Format( "Got PICS changelist {0} for {1} apps and {2} packages - {3}",
                    changeList.ChangeNumber, changeList.Apps.Count, changeList.Packages.Count, GetChangelistURL( changeList.ChangeNumber ) );

                if ( numPackageChanges >= ChangesReqToBeImportant || numAppChanges >= ChangesReqToBeImportant )
                {
                    // if this changelist contains a number of changes over a specific threshold, we'll consider it "important" and send to all channels
                    IRC.Instance.SendToTag( "pics", message );
                }

                if ( numAppChanges > 0 )
                {
                    var importantApps = changeList.Apps.Select( a => a.ID )
                        .Intersect( Settings.Current.ImportantApps.Select( a => a.AppID ) );

                    foreach ( var app in importantApps )
                    {
                        SettingsXml.ImportantApp importantApp = Settings.Current.ImportantApps
                            .FirstOrDefault( a => a.AppID == app );

                        string tag = "pics";

                        if ( !string.IsNullOrEmpty( importantApp.Tag ) )
                        {
                            tag = string.Format( "{0}-{1}", tag, importantApp.Tag );
                        }

                        // get the channels interested in this pics update
                        var picsChannels = Settings.Current.GetChannelsForTag( tag );

                        if ( picsChannels.Count() == 0 )
                        {
                            Log.WriteWarn( "PICSJob", "No channels setup for tag: {0}", tag );
                        }

                        string targetChans = string.Join( ",", picsChannels.Select( c => c.Channel ) );

                        IRC.Instance.Send( targetChans, "Important App Update: {0} - {1}", Steam.Instance.GetAppName( app ), GetAppHistoryUrl( app ) );
                    }
                }

                if ( numPackageChanges > 0 )
                {
                    // todo: important packages
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
