using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;

namespace SteamIrcBot
{
    static class SmartIrc4NetExtensions
    {
        public static Priority ToSmartIrcPriority( this SendPriority prio )
        {
            switch ( prio )
            {
                case SendPriority.Low:
                    return Priority.Low;

                case SendPriority.Medium:
                    return Priority.Medium;

                case SendPriority.High:
                    return Priority.High;

                case SendPriority.Critical:
                    return Priority.Critical;
            }

            return Priority.Medium;
        }
    }

    class SmartIrc4NetClient : IIrcClient
    {
        IrcClient client = new IrcClient();


        public SmartIrc4NetClient()
        {
            client.SendDelay = 0;
            client.Encoding = Encoding.UTF8;
            client.AutoRetry = true;
            client.AutoRejoin = true;
            client.AutoRelogin = true;
            client.AutoRejoinOnKick = true;

            // todo: this causes issues when netsplits occur, but we need it for channel user objects
            // it can probably be implemented better by handling NAMES response
            client.ActiveChannelSyncing = true;
        }


        public bool IsConnected
        {
            get { return client.IsConnected; }
        }


        public void Connect( string server, int port )
        {
            client.Connect( server, port );
        }

        public void Disconnect()
        {
            client.Disconnect();
        }

        public void Login( string username, string realname, string nickname, string password = null )
        {
            client.Login( nickname, realname, 4, username, password );
        }

        public void SendRaw( string raw, SendPriority priority = SendPriority.Medium )
        {
            client.WriteLine( raw, priority.ToSmartIrcPriority() );
        }

        public void SendEmote( string target, string emote, SendPriority priority = SendPriority.Medium )
        {
            client.SendMessage( Meebey.SmartIrc4net.SendType.Action, target, emote, priority.ToSmartIrcPriority() );
        }

        public void SendMessage( string target, string message, SendPriority priority = SendPriority.Medium )
        {
            client.SendMessage( Meebey.SmartIrc4net.SendType.Message, target, message, priority.ToSmartIrcPriority() );
        }

        public void Join( string[] channels, SendPriority priority = SendPriority.Medium )
        {
            client.RfcJoin( channels, priority.ToSmartIrcPriority() );
        }

        public bool IsUserOnChannel( string user, string channel )
        {
            ChannelUser userObj = client.GetChannelUser( channel, user );
            return userObj != null;
        }

        public bool IsMe( string user )
        {
            return client.IsMe( user );
        }

        public void Tick()
        {
            client.ListenOnce( false );
        }

        public event EventHandler<IrcMessageEventArgs> OnMessage;
        public event EventHandler<IrcJoinEventArgs> OnJoin;
    }
}
