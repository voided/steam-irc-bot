using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using SteamKit2;
using SteamKit2.Internal;

namespace SteamIrcBot
{
    class SteamLevels : ClientMsgHandler
    {
        public class SteamLevelsCallback : CallbackMsg
        {
            public class Friend
            {
                public SteamID FriendID { get; private set; }
                public uint Level { get; private set; }

                internal Friend( CMsgClientFSGetFriendsSteamLevelsResponse.Friend friend )
                {
                    FriendID = new SteamID( friend.accountid, EUniverse.Public, EAccountType.Individual );
                    Level = friend.level;
                }
            }


            public ReadOnlyCollection<Friend> Friends { get; private set; }


            internal SteamLevelsCallback( CMsgClientFSGetFriendsSteamLevelsResponse resp )
            {
                Friends = new ReadOnlyCollection<Friend>( resp.friends.Select( f => new Friend( f ) ).ToList() );
            }
        }


        public JobID RequestLevels( IEnumerable<SteamID> steamIds )
        {
            var clientMsg = new ClientMsgProtobuf<CMsgClientFSGetFriendsSteamLevels>( EMsg.ClientFSGetFriendsSteamLevels );
            clientMsg.SourceJobID = Client.GetNextJobID();

            clientMsg.Body.accountids.AddRange( steamIds.Select( s => s.AccountID ) );

            Client.Send( clientMsg );

            return clientMsg.SourceJobID;
        }

        public override void HandleMsg( IPacketMsg packetMsg )
        {
            switch ( packetMsg.MsgType )
            {
                case EMsg.ClientFSGetFriendsSteamLevelsResponse:
                    HandleSteamLevelsResponse( packetMsg );
                    break;
            }
        }

        void HandleSteamLevelsResponse( IPacketMsg msg )
        {
            var clientMsg = new ClientMsgProtobuf<CMsgClientFSGetFriendsSteamLevelsResponse>( msg );

            var innerCallback = new SteamLevelsCallback( clientMsg.Body );
            var jobCallback = new SteamClient.JobCallback<SteamLevelsCallback>( clientMsg.TargetJobID, innerCallback );

            Client.PostCallback( jobCallback );
        }
    }
}
