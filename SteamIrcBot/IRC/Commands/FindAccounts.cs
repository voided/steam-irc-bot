using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamIrcBot
{
    class FindAccounts : Command<FindAccounts.Request>
    {
        public class Request : BaseRequest
        {
            public JobID JobID { get; set; }
        }

        public FindAccounts()
        {
            Triggers.Add( "!findacc" );
            HelpText = "!findacc <query> <type> - Requests a list of accounts by input";

            new Callback<SteamAccount.ResponseCallback>( OnAccountInfo, Steam.Instance.CallbackManager );
        }

        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length < 2 )
            {
                IRC.Instance.Send( details.Channel, "{0}: Usage !findacc <query> <type>", details.Sender.Nickname );
                return;
            }

            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to request account info: not connected to Steam!", details.Sender.Nickname );
                return;
            }

            string accOrEmail = details.Args[ 0 ];
            string typeStr = details.Args[ 1 ];

            SteamAccount.ERequestAccountData type;
            if ( !Enum.TryParse( typeStr, out type ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid type, should be one of: {1}",
                    details.Sender.Nickname, string.Join( ", ", Enum.GetNames( typeof( SteamAccount.ERequestAccountData ) ) ) );
                return;
            }

            var jobId = Steam.Instance.Account.RequestAccountData( accOrEmail, type );
            AddRequest( details, new Request { JobID = jobId } );
        }


        void OnAccountInfo( SteamAccount.ResponseCallback callback )
        {
            var req = GetRequest( r => r.JobID == callback.JobID );

            if ( req == null )
                return;

            if ( callback.Result != EResult.OK )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request account info: {1}", req.Requester.Nickname, callback.Result );
                return;
            }

            IRC.Instance.Send( req.Channel, "{0}: Count matches: {1}, Account name: {2}",
                req.Requester.Nickname, callback.CountMatches, string.IsNullOrEmpty( callback.AccountName ) ? "none" : callback.AccountName
            );
        }
    }
}
