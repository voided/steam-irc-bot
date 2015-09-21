using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamIrcBot
{
    abstract class Command
    {
        public List<string> Triggers { get; set; }

        public string HelpText { get; set; }


        protected Command()
        {
            Triggers = new List<string>();
        }


        internal void DoRun( CommandDetails details )
        {
            OnRun( details );
        }

        protected abstract void OnRun( CommandDetails details );
    }

    interface IRequestableCommand
    {
        void ExpireRequests();
    }

    abstract class Command<TReq> : Command, IRequestableCommand
        where TReq : Command<TReq>.BaseRequest
    {
        public abstract class BaseRequest
        {
            public string Name { get; set; }

            public SenderDetails Requester { get; set; }
            public string Channel { get; set; }

            public DateTime ExpireTime { get; set; }


            public override string ToString()
            {
                return string.Format( "{0} for {1} in {2}", Name, Requester, Channel );
            }
        }

        protected List<TReq> Requests { get; private set; }


        public Command()
        {
            Requests = new List<TReq>();
        }


        protected void AddRequest( CommandDetails details, TReq req )
        {
            req.Name = details.Trigger;

            req.Channel = details.Channel;
            req.Requester = details.Sender;

            req.ExpireTime = DateTime.Now + TimeSpan.FromSeconds( 5 );

            Requests.Add( req );

            Log.WriteDebug( "Command", "Created request {0}: {1}", typeof( TReq ), req );
        }

        protected TReq GetRequest( Func<TReq, bool> predicate )
        {
            var req = Requests
                .FirstOrDefault( predicate );

            if ( req != null )
            {
                Requests.Remove( req );
            }
           
            return req;
        }


        protected virtual void OnExpire( TReq request )
        {
            IRC.Instance.Send( request.Channel, "{0}: Your {1} request has timed out.", request.Requester.Nickname, request.Name );
        }

        public void ExpireRequests()
        {
            var expiredReqs = Requests
                .Where( req => DateTime.Now >= req.ExpireTime )
                .ToList(); // copy list because expire actions could modify original request list

            foreach ( var req in expiredReqs )
            {
                OnExpire( req );

                Requests.Remove( req );
            }
        }

        protected void ShowHelp( CommandDetails details )
        {
            IRC.Instance.Send( details.Channel, "{0}: {1}", details.Sender.Nickname, HelpText );
        }
    }

    abstract class DotaCommand<TReq> : Command<TReq>
        where TReq : DotaCommand<TReq>.DotaBaseRequest
    {
        public abstract class DotaBaseRequest : BaseRequest
        {
            public JobID Job { get; set; }
        }

        public const int APPID = 570;
    }

    class CommandDetails
    {
        public string Trigger { get; set; }
        public string[] Args { get; set; }

        public SenderDetails Sender { get; set; }
        public string Channel { get; set; }
    }
}
