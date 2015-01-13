using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SteamIrcBot
{
    class IRC
    {
        static IRC _instance = new IRC();
        public static IRC Instance { get { return _instance; } }


        public CommandManager CommandManager { get; private set; }


        IIrcClient client;
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
            client = new SmartIrc4NetClient();

            nextConnect = DateTime.MaxValue;

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

            SendToTag( "main", "Shutting down!" );
            client.SendRaw( "QUIT :fork it all", SendPriority.Critical );
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

            InternalSendMessage( SendType.Message, targetString, string.Format( format, args ) );
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

            InternalSendMessage( SendType.Action, targetString, string.Format( format, args ) );
        }

        public void Send( string channel, string format, params object[] args )
        {
            if ( !Connected )
                return;

            InternalSendMessage( SendType.Message, channel, string.Format( format, args ) );
        }
        public void SendEmote( string channel, string format, params object[] args )
        {
            if ( !Connected )
                return;

            InternalSendMessage( SendType.Action, channel, string.Format( format, args ) );
        }

        void InternalSendMessage( SendType sendType, string target, string message )
        {
            // remove all possible newline characters
            message = message
                .Replace( "\n", "" )
                .Replace( "\r", "" );

            // maximum amount of text we want to allow in a message until we split it into chunks
            const int MAX_LINE = 400;

            do
            {
                var messageChunk = message.Take( MAX_LINE ).ToActualString();
                message = message.Skip( MAX_LINE ).ToActualString();

                switch ( sendType )
                {
                    case SendType.Message:
                        client.SendMessage( target, messageChunk );
                        break;

                    case SendType.Emote:
                        client.SendEmote( target, messageChunk );
                        break;
                }
            }
            while ( message.Length > 0 );
        }

        public void Join( string[] channels )
        {
            client.Join( channels );
        }

        public bool IsUserOnChannel( string channel, string user )
        {
            return client.IsUserOnChannel( user, channel );
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

                if ( !string.IsNullOrEmpty( Settings.Current.IRCPassword ) )
                {
                    client.Login( "steamircbot", Settings.Current.IRCNick, Settings.Current.IRCNick, Settings.Current.IRCPassword );
                }
                else
                {
                    client.Login( "steamircbot", Settings.Current.IRCNick, Settings.Current.IRCNick );
                }

            }

            client.Tick();

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

            client.Join( Settings.Current.IRCChannels.Select( chan => chan.Channel ).ToArray() );
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

        void OnJoin( object sender, IrcJoinEventArgs e )
        {
            var mainChan = Settings.Current.GetChannelsForTag( "main" )
                .FirstOrDefault();

            if ( mainChan == null )
            {
                Log.WriteWarn( "IRC", "No main channel configured, won't connect to Steam!" );
                return;
            }

            // todo: implement this
            if ( client.IsMe( e.Who.Nickname ) && string.Equals( e.Channel, mainChan.Channel, StringComparison.OrdinalIgnoreCase ) )
            {
                if ( !Steam.Instance.Connected )
                    Steam.Instance.Connect();
            }
        }
    }
}
