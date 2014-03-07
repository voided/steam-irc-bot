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


        public void SendToTag( string tag, string format, params object[] args )
        {
            if ( !Connected )
                return;

            var chans = Settings.Current.GetChannelsForTag( tag );

            if ( chans.Count() == 0 )
            {
                Log.WriteWarn( "IRC", "Tag {0} has no channels!", tag );
                return;
            }

            string targetString = string.Join( ",", chans.Select( c => c.Channel ) );

            client.SendMessage( SendType.Message, targetString, string.Format( format, args ) );
        }
        public void SendEmoteToTag( string tag, string format, params object[] args )
        {
            if ( !Connected )
                return;

            var chans = Settings.Current.GetChannelsForTag( tag );

            if ( chans.Count() == 0 )
            {
                Log.WriteWarn( "IRC", "Tag {0} has no channels!", tag );
                return;
            }

            string targetString = string.Join( ",", chans.Select( c => c.Channel ) );

            client.SendMessage( SendType.Action, targetString, string.Format( format, args ) );
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

            client.RfcJoin( Settings.Current.Channels.Select( chan => chan.Channel ).ToArray() );
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
            var mainChan = Settings.Current.GetChannelsForTag( "main" )
                .FirstOrDefault();

            if ( mainChan == null )
            {
                Log.WriteWarn( "IRC", "No main channel configured, won't connect to Steam!" );
                return;
            }

            if ( e.Data.Nick == client.Nickname && string.Equals( e.Channel, mainChan.Channel, StringComparison.OrdinalIgnoreCase ) )
            {
                if ( !Steam.Instance.Connected )
                    Steam.Instance.Connect();
            }
        }
    }
}
