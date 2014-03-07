using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Meebey.SmartIrc4net;

namespace SteamIrcBot
{
    class SenderDetails
    {
        public string Nickname { get; set; }
        public string Ident { get; set; }
        public string Hostname { get; set; }

        public override string ToString()
        {
            return string.Format( "{0}!{1}@{2}", Nickname, Ident, Hostname );
        }
    }

    class IRC
    {
        static IRC _instance = new IRC();
        public static IRC Instance { get { return _instance; } }


        public CommandManager CommandManager { get; private set; }


        IrcClient client = new IrcClient();
        bool shuttingDown = false;

        DateTime nextConnect;
        public bool Connected
        {
            get
            {
                if ( !client.IsConnected )
                    return false;

                if ( nextConnect != DateTime.MaxValue )
                    return false; // if we have a connection scheduled, we're not connected

                return true;
            }
        }



        IRC()
        {
            nextConnect = DateTime.MaxValue;

            client.SendDelay = ( int )TimeSpan.FromSeconds( 0.5 ).TotalMilliseconds;
            client.Encoding = Encoding.UTF8;
            client.AutoRetry = true;
            client.AutoRejoin = true;
            client.AutoRejoinOnKick = true;
            client.ActiveChannelSyncing = true;

            CommandManager = new CommandManager( client );

            client.OnRegistered += OnConnected;
            client.OnDisconnected += OnDisconnected;
            client.OnJoin += OnJoin;
        }


        public void Connect()
        {
            CommandManager.Init();

            if ( client.IsConnected )
                return;

            Reconnect( TimeSpan.Zero );
        }

        public void Disconnect()
        {
            if ( !client.IsConnected )
                return;

            Log.WriteInfo( "IRC", "Disconnecting..." );

            shuttingDown = true;

            client.WriteLine( "QUIT :fork it all", Priority.Critical );
        }


        public void Send( string channel, string format, params object[] args )
        {
            if ( !Connected )
                return;

            client.SendMessage( SendType.Message, channel, string.Format( format, args ) );
        }
        public void SendEmote( string channel, string format, params object[] args )
        {
            if ( !Connected )
                return;

            client.SendMessage( SendType.Action, channel, string.Format( format, args ) );
        }
        public void SendAnnounce( string format, params object[] args )
        {
            Send( Settings.Current.IRCAnnounceChannel, format, args );
        }
        public void SendEmoteAnnounce( string format, params object[] args )
        {
            SendEmote( Settings.Current.IRCAnnounceChannel, format, args );
        }
        public void SendAll( string format, params object[] args )
        {
            Send( string.Format( "{0},{1}", Settings.Current.IRCMainChannel, Settings.Current.IRCAnnounceChannel ), format, args );
        }
        public void SendEmoteAll( string format, params object[] args )
        {
            SendEmote( string.Format( "{0},{1}", Settings.Current.IRCMainChannel, Settings.Current.IRCAnnounceChannel ), format, args );
        }

        public void Join( string[] channels )
        {
            client.RfcJoin( channels );
        }

        public bool IsUserOnChannel( string channel, string user )
        {
            ChannelUser userObj = client.GetChannelUser( channel, user );
            return userObj != null;
        }


        public void Tick()
        {
            if ( DateTime.Now >= nextConnect )
            {
                nextConnect = DateTime.MaxValue;

                Log.WriteInfo( "IRC", "Connecting..." );

                if ( client.IsConnected )
                    client.Disconnect();

                client.Connect( Settings.Current.IRCServer, Settings.Current.IRCPort );

                var nickList = new string[] { Settings.Current.IRCNick, Settings.Current.IRCNick + "_" };
                client.Login( nickList, Settings.Current.IRCNick, 4, "steamircbot" );

            }

            client.ListenOnce( false );

            CommandManager.Tick();
        }

        void Reconnect( TimeSpan when )
        {
            Log.WriteInfo( "IRC", "Reconnecting in {0}", when );
            nextConnect = DateTime.Now + when;
        }


        void OnConnected( object sender, EventArgs e )
        {
            Log.WriteInfo( "IRC", "Connected!" );

            client.RfcJoin( new string[] { Settings.Current.IRCMainChannel, Settings.Current.IRCAnnounceChannel, Settings.Current.IRCAuxChannel } );
        }

        void OnDisconnected( object sender, EventArgs e )
        {
            if ( shuttingDown )
            {
                Log.WriteInfo( "IRC", "Disconnected due to service shutdown" );
                return;
            }

            Log.WriteInfo( "IRC", "Disconnected!" );

            Reconnect( TimeSpan.FromSeconds( 30 ) );
        }

        void OnJoin( object sender, JoinEventArgs e )
        {
            if ( e.Data.Nick == client.Nickname && e.Channel == Settings.Current.IRCMainChannel )
            {
                if ( !Steam.Instance.Connected )
                    Steam.Instance.Connect();
            }
        }
    }
}
