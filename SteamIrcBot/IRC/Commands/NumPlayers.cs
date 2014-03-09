using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

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

            Triggers.Add( "!numplayers" );
            HelpText = "!numplayers <gameid> - Requests the current number of players playing the given GameID, according to Steam";
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

            GameID realGameID = gameId;

            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to request player counts for {1}: not connected to Steam!", details.Sender.Nickname, GetAppName( realGameID.AppID ) );
                return;
            }

            if ( realGameID.AppID == 0 )
            {
                // send this request as a package info request
                Steam.Instance.Apps.PICSGetProductInfo( null, realGameID.AppID, false, false );
            }
            else
            {
                // send off a product request as well so we get something to cache for later
                Steam.Instance.Apps.PICSGetProductInfo( realGameID.AppID, null, false, false );
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
                IRC.Instance.Send( req.Channel, "{0}: Unable to request player counts for {1}: {2}", req.Requester.Nickname, GetAppName( req.GameID.AppID ), callback.Result );
                return;
            }

            IRC.Instance.Send( req.Channel, "{0}: {1} players: {2}", req.Requester.Nickname, GetAppName( req.GameID.AppID ), callback.NumPlayers );
        }


        string GetAppName( uint appId )
        {
            if ( appId == 0 )
            {
                // steam tracks player counts for appid 0 as "Steam"
                // not using the package name because sumbaudy at valve renamed all the packages to Steam Sub #
                return "Steam";
            }

            return Steam.Instance.GetAppName( appId );
        }
    }
}
