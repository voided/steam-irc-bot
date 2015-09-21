using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;

namespace SteamIrcBot
{
    class DotaPlayers : DotaCommand<DotaPlayers.Request>
    {
        UGCHandler ugcHandler;

        public class Request : DotaBaseRequest
        {
        }

        public DotaPlayers()
        {
            Triggers.Add( "!dotaplayers" );
            HelpText = "!dotaplayers - Requests player count information for a dota custom game";

            new GCCallback<CMsgGCToClientCustomGamePlayerCountResponse>( (uint)EDOTAGCMsg.k_EMsgGCToClientCustomGamePlayerCountResponse, OnPlayers, Steam.Instance.GCManager );

            ugcHandler = Steam.Instance.SteamManager.GetHandler<UGCHandler>();
        }

        protected override void OnRun( CommandDetails details )
        {
            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to request dota custom game player information: not connected to Steam!", details.Sender.Nickname );
                return;
            }

            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: Game ID argument required", details.Sender.Nickname );
                return;
            }

            ulong gameId;
            if ( !ulong.TryParse( details.Args[ 0 ], out gameId ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid Game ID", details.Sender.Nickname );
                return;
            }

            var request = new ClientGCMsgProtobuf<CMsgClientToGCCustomGamePlayerCountRequest>( (uint)EDOTAGCMsg.k_EMsgClientToGCCustomGamePlayerCountRequest );

            request.Body.custom_game_id = gameId;
            request.SourceJobID = Steam.Instance.Client.GetNextJobID();

            Steam.Instance.GameCoordinator.Send( request, APPID );

            AddRequest( details, new Request { Job = request.SourceJobID } );
        }

        void OnPlayers( ClientGCMsgProtobuf<CMsgGCToClientCustomGamePlayerCountResponse> response, uint appId )
        {
            var req = GetRequest( r => r.Job == response.TargetJobID );

            if ( req == null )
                return;

            ulong gameId = response.Body.custom_game_id;

            string gameName;

            if ( !ugcHandler.LookupUGCName( gameId, out gameName ) )
            {
                // no name cached, just use the pubfile id for now
                gameName = gameId.ToString();
            } 

            ulong players = response.Body.player_count;
            ulong spectators = response.Body.spectator_count;

            IRC.Instance.Send( req.Channel, "{0}: {1} has {2} players and {3} spectators", req.Requester.Nickname, gameName, players, spectators );
        }
    }
}
