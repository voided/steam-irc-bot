using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HeronIRC;

namespace SteamIrcBot
{
    abstract class Command
    {
        public string Trigger { get; set; }

        public string HelpText { get; set; }


        internal void DoRun( CommandDetails details )
        {
            OnRun( details );
        }

        protected abstract void OnRun( CommandDetails details );
    }

    abstract class Command<TReq> : Command
        where TReq : Command<TReq>.BaseRequest
    {
        public abstract class BaseRequest
        {
            public string Name { get; set; }

            public Hostmask Requester { get; set; }
            public string Channel { get; set; }

            public DateTime ExpireTime { get; set; }
        }

        protected List<TReq> Requests { get; private set; }


        public Command()
        {
            Requests = new List<TReq>();
        }


        protected void AddRequest( CommandDetails details, TReq req )
        {
            ExpireRequests();

            req.Name = details.Trigger;

            req.Channel = details.Channel;
            req.Requester = details.Sender;

            req.ExpireTime = DateTime.Now + TimeSpan.FromSeconds( 5 );

            Requests.Add( req );
        }

        protected TReq GetRequest( Func<TReq, bool> predicate )
        {
            var req = Requests
                .FirstOrDefault( predicate );

            if ( req != null )
                Requests.Remove( req );

            ExpireRequests();

            return req;
        }


        protected virtual void OnExpire( TReq request )
        {
            IRC.Instance.Send( request.Channel, "{0}: Your {1} request has timed out.", request.Requester.Nickname, request.Name );
        }


        void ExpireRequests()
        {
            var expiredReqs = Requests
                .Where( req => DateTime.Now >= req.ExpireTime );

            foreach ( var req in expiredReqs )
            {
                OnExpire( req );
            }

            Requests
                .RemoveAll( req => DateTime.Now >= req.ExpireTime );
        }
    }

    class CommandDetails
    {
        public string Trigger { get; set; }
        public string[] Args { get; set; }

        public Hostmask Sender { get; set; }
        public string Channel { get; set; }
    }
}
