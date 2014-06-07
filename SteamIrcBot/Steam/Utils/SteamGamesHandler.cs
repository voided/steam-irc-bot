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
        public void PlayGames( IEnumerable<uint> games )
        {
            var clientMsg = new ClientMsgProtobuf<CMsgClientGamesPlayed>( EMsg.ClientGamesPlayedNoDataBlob );

            clientMsg.Body.games_played.AddRange(
                games.Select( g => new CMsgClientGamesPlayed.GamePlayed
                {
                    game_id = g
                } )
            );

            Client.Send( clientMsg );
        }


        public override void HandleMsg( IPacketMsg packetMsg )
        {
            // nop
        }
    }
}
