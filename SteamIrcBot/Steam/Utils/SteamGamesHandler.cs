using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;
using SteamKit2.Internal;

namespace SteamIrcBot
{
    class SteamGames : ClientMsgHandler
    {
        public void PlayGame( GameID game )
        {
            var clientMsg = new ClientMsgProtobuf<CMsgClientGamesPlayed>( EMsg.ClientGamesPlayedNoDataBlob );

            clientMsg.Body.games_played.Add( new CMsgClientGamesPlayed.GamePlayed
            {
                game_id = game,
            } );

            Client.Send( clientMsg );
        }


        public override void HandleMsg( IPacketMsg packetMsg )
        {
            // nop
        }
    }
}
