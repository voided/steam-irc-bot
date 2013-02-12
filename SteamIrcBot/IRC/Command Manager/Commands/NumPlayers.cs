using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;
using HeronIRC;

namespace SteamIrcBot
{
    class NumPlayersCommand : Command<NumPlayersCommand.Request>
    {
        public class Request : BaseRequest
        {
            public JobID JobID { get; set; }

            public GameID GameID { get; set; }
        }


        public NumPlayersCommand()
        {
            new JobCallback<SteamUserStats.NumberOfPlayersCallback>( OnNumPlayers, Steam.Instance.CallbackManager );

            Trigger = "!numplayers";
        }


        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: GameID argument required", details.Sender.Nickname );
                return;
            }
            
            ulong gameId;
            if ( !ulong.TryParse( details.Args[ 0 ], out gameId ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid GameID", details.Sender.Nickname );
                return;
            }

            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, details.Sender.Nickname + ": Unable to request player counts: not connected to Steam!" );
                return;
            }

            var jobId = Steam.Instance.UserStats.GetNumberOfCurrentPlayers( gameId );
            AddRequest( details, new Request { JobID = jobId, GameID = gameId } );
        }


        void OnNumPlayers( SteamUserStats.NumberOfPlayersCallback callback, JobID jobId )
        {
            var req = GetRequest( r => r.JobID == jobId );

            if ( req == null )
                return;

            if ( callback.Result != EResult.OK )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request players: {1}", req.Requester.Nickname, callback.Result );
                return;
            }

            IRC.Instance.Send( req.Channel, "{0}: {1} players: {2}", req.Requester.Nickname, req.GameID, callback.NumPlayers );
        }
    }
}
