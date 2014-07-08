using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;
using SteamKit2.Internal;

namespace SteamIrcBot
{
    class SteamAccount : ClientMsgHandler
    {
        public class ResponseCallback : CallbackMsg
        {
            public EResult Result { get; private set; }

            public ERequestAccountData Action { get; private set; }

            public uint CountMatches { get; private set; }
            public string AccountName { get; private set; }


            internal ResponseCallback( JobID jobID, CMsgClientRequestAccountDataResponse msg )
            {
                this.JobID = jobID;

                Result = ( EResult )msg.eresult;
                Action = ( ERequestAccountData )msg.action;

                CountMatches = msg.ct_matches;
                AccountName = msg.account_name;
            }
        }

        public enum ERequestAccountData
        {
            FindAccountByEmail = 1,
            FindAccountByCdKey = 2,
            GetNumAccountsWithEmailAddress = 3,
            IsAccountNameInUse = 4,
        };


        public JobID RequestAccountData( string accOrEmail, ERequestAccountData requestType )
        {
            var request = new ClientMsgProtobuf<CMsgClientRequestAccountData>( EMsg.ClientRequestAccountData );
            request.SourceJobID = Client.GetNextJobID();

            request.Body.account_or_email = accOrEmail;
            request.Body.action = ( uint )requestType;

            Client.Send( request );

            return request.SourceJobID;
        }

        public override void HandleMsg( IPacketMsg packetMsg )
        {
            switch ( packetMsg.MsgType )
            {
                case EMsg.ClientRequestAccountDataResponse:
                    HandleAccountDataResponse( packetMsg );
                    break;
            }
        }


        void HandleAccountDataResponse( IPacketMsg packetMsg )
        {
            var msg = new ClientMsgProtobuf<CMsgClientRequestAccountDataResponse>( packetMsg );

            var callback = new ResponseCallback( msg.TargetJobID, msg.Body );

            Client.PostCallback( callback );
        }
    }
}
