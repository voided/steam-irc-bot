using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;

namespace SteamIrcBot
{
    abstract class BaseMonitor
    {
        internal void DoMessage( MessageDetails msgDetails )
        {
            OnMessage( msgDetails );
        }

        protected abstract void OnMessage( MessageDetails msgDetails );
    }

    class MessageDetails
    {
        public string Channel { get; private set; }
        public SenderDetails Sender { get; private set; }

        public string Message { get; private set; }


        public MessageDetails( string channel, SenderDetails sender, string message )
        {
            this.Channel = channel;
            this.Sender = sender;
            this.Message = message;
        }
    }
}
