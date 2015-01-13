using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    interface IIrcClient
    {
        bool IsConnected { get; }


        void Connect( string server, int port );
        void Disconnect();

        void Login( string username, string realname, string nickname, string password = null );

        void SendRaw( string raw, SendPriority priority = SendPriority.Medium );

        void SendEmote( string target, string emote, SendPriority priority = SendPriority.Medium );
        void SendMessage( string target, string message, SendPriority priority = SendPriority.Medium );

        void Join( string[] channels, SendPriority priority = SendPriority.Medium );

        // THIS SUCKS TO IMPLEMENT
        // and maybe we don't actually need it
        bool IsUserOnChannel( string user, string channel );

        bool IsMe( string user );

        void Tick();


        event EventHandler<IrcMessageEventArgs> OnMessage;
        event EventHandler<IrcJoinEventArgs> OnJoin;
    }

    class IrcMessageEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public SenderDetails Sender { get; private set; }

        public string Source { get; private set; }


        public IrcMessageEventArgs( string source, SenderDetails sender, string message )
        {
            this.Source = source;
            this.Sender = sender;
            this.Message = message;
        }
    }

    class IrcJoinEventArgs : EventArgs
    {
        public string Channel { get; private set; }

        public SenderDetails Who { get; private set; }


        public IrcJoinEventArgs( string channel, SenderDetails who )
        {
            this.Channel = channel;
            this.Who = who;
        }
    }

    enum SendType
    {
        Message,
        Emote,

        Action = Emote,
    }

    enum SendPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
