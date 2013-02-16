using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using SteamKit2;

namespace SteamIrcBot
{
    class Steam
    {
        static Steam _instance = new Steam();
        public static Steam Instance { get { return _instance; } }


        public SteamClient Client { get; private set; }

        public CallbackManager CallbackManager { get; private set; }
        public GCManager GCManager { get; private set; }

        public JobManager JobManager { get; private set; }

        public SteamUser User { get; private set; }
        public SteamFriends Friends { get; private set; }
        public SteamApps Apps { get; private set; }
        public SteamUserStats UserStats { get; private set; }
        public SteamLevels Levels { get; private set; }
        public SteamGames Games { get; private set; }
        public SteamAppInfo AppInfo { get; private set; }
        public SteamAccount Account { get; private set; }


        bool loggedOn;
        public bool Connected { get { return Client.ConnectedUniverse != EUniverse.Invalid && loggedOn; } }


        Steam()
        {
            Client = new SteamClient();

            CallbackManager = new CallbackManager( Client );

            GCManager = new GCManager( CallbackManager );
            JobManager = new JobManager( CallbackManager );

            User = Client.GetHandler<SteamUser>();
            Friends = Client.GetHandler<SteamFriends>();
            Apps = Client.GetHandler<SteamApps>();
            UserStats = Client.GetHandler<SteamUserStats>();
            Levels = new SteamLevels();
            Games = new SteamGames();
            AppInfo = new SteamAppInfo();
            Account = new SteamAccount();

            Client.AddHandler( Levels );
            Client.AddHandler( Games );
            Client.AddHandler( AppInfo );
            Client.AddHandler( Account );

            new Callback<SteamClient.ConnectedCallback>( OnConnected, CallbackManager );
            new Callback<SteamClient.DisconnectedCallback>( OnDisconnected, CallbackManager );

            new Callback<SteamUser.LoggedOnCallback>( OnLoggedOn, CallbackManager );
            new Callback<SteamUser.LoggedOffCallback>( OnLoggedOff, CallbackManager );
        }


        public void Connect()
        {
            JobManager.Start();

            Log.WriteInfo( "Steam", "Connecting..." );

            IRC.Instance.SendAnnounce( "Connecting to Steam..." );

            Client.Connect();
        }

        public void Disconnect()
        {
            Log.WriteInfo( "Steam", "Disconnecting..." );

            Client.Disconnect();

            JobManager.Stop();
        }


        public string GetAppName( uint appId )
        {
            string appName;

            if ( !AppInfo.GetAppName( appId, out appName ) )
                return appId.ToString();

            return string.Format( "{0} ({1})", appName, appId );
        }

        public string GetPackageName( uint packageId )
        {
            string packageName;

            if ( !AppInfo.GetPackageName( packageId, out packageName ) )
                return packageId.ToString();

            return string.Format( "{0} ({1})", packageName, packageId );
        }


        void OnConnected( SteamClient.ConnectedCallback callback )
        {
            loggedOn = false;

            if ( callback.Result != EResult.OK )
            {
                Log.WriteWarn( "Steam", "Unable to connect to Steam3: {0}", callback.Result );

                IRC.Instance.SendAnnounce( "Unable to connect to Steam: {0}", callback.Result );
                return;
            }

            Log.WriteInfo( "Steam", "Connected to Steam3: {0}", callback.Result );

            IRC.Instance.SendAnnounce( "Connected to Steam! Logging on..." );

            User.LogOn( new SteamUser.LogOnDetails
            {
                Username = Settings.Current.SteamUsername,
                Password = Settings.Current.SteamPassword
            } );
        }
        void OnDisconnected( SteamClient.DisconnectedCallback callback )
        {
            loggedOn = false;

            Log.WriteInfo( "Steam", "Disconnected from Steam" );

            IRC.Instance.SendAnnounce( "Disconnected from Steam! Reconnecting..." );

            Client.Connect();
        }

        void OnLoggedOn( SteamUser.LoggedOnCallback callback )
        {
            if ( callback.Result != EResult.OK )
            {
                Log.WriteWarn( "Steam", "Unable to logon to Steam3: {0} / {1}", callback.Result, callback.ExtendedResult );

                IRC.Instance.SendAnnounce( "Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult );
                return;
            }

            loggedOn = true;

            Log.WriteInfo( "Steam", "Logged onto Steam3!" );

            IRC.Instance.SendAll( "Logged on to Steam! Server time: {0}", callback.ServerTime );

            Games.PlayGame( 440 );
        }

        void OnLoggedOff( SteamUser.LoggedOffCallback callback )
        {
            Log.WriteWarn( "Steam", "Logged off Steam3: {0}", callback.Result );

            IRC.Instance.SendAll( "Logged off of Steam: {0}", callback.Result );

            Client.Disconnect();
        }
    }
}
