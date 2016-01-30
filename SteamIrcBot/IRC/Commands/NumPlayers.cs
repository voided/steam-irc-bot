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

            public uint AppID { get; set; }
        }


        public NumPlayersCommand()
        {
            Steam.Instance.CallbackManager.Subscribe<SteamUserStats.NumberOfPlayersCallback>( OnNumPlayers );

            Triggers.Add( "!numplayers" );
            Triggers.Add( "!players" );
            HelpText = "!numplayers <appid/name> - Requests the current number of players playing the given GameID or app name, according to Steam";
        }


        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: AppID argument required", details.Sender.Nickname );
                return;
            }
            
            uint appId;
            if ( !uint.TryParse( details.Args[ 0 ], out appId ) )
            {
                // lets search by name if we're not a gameid

                string appName = string.Join( " ", details.Args );

                if ( !Steam.Instance.AppInfo.FindApp( appName, out appId, gamesOnly: true ) )
                {
                    IRC.Instance.Send( details.Channel, "{0}: Invalid GameID or unknown app name", details.Sender.Nickname );
                    return;
                }
            }

            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to request player counts for {1}: not connected to Steam!", details.Sender.Nickname, GetAppName( appId ) );
                return;
            }

            if ( appId == 0 )
            {
                // send this request as a package info request
                Steam.Instance.Apps.PICSGetProductInfo( null, appId, false, false );
            }
            else
            {
                // send off a product request as well so we get something to cache for later
                Steam.Instance.Apps.PICSGetProductInfo( appId, null, false, false );
            }

            var jobId = Steam.Instance.UserStats.GetNumberOfCurrentPlayers( appId );
            AddRequest( details, new Request { JobID = jobId, AppID = appId } );
        }


        void OnNumPlayers( SteamUserStats.NumberOfPlayersCallback callback )
        {
            var req = GetRequest( r => r.JobID == callback.JobID );

            if ( req == null )
                return;

            if ( callback.Result != EResult.OK )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request player counts for {1}: {2}", req.Requester.Nickname, GetAppName( req.AppID ), callback.Result );
                return;
            }

            IRC.Instance.Send( req.Channel, "{0}: {1} players: {2}", req.Requester.Nickname, GetAppName( req.AppID ), callback.NumPlayers );
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
