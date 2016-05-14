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

        PlayerCountHandler playerCountHandler;


        public NumPlayersCommand()
        {
            playerCountHandler = Steam.Instance.SteamManager.GetHandler<PlayerCountHandler>();

            Steam.Instance.CallbackManager.Subscribe<SteamUserStats.NumberOfPlayersCallback>( OnNumPlayers );

            Triggers.Add( "!numplayers" );
            Triggers.Add( "!players" );
            HelpText = "!numplayers <appid/name> - Requests the current number of players playing the given GameID or app name, according to Steam";
        }


        protected async override void OnRun( CommandDetails details )
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
                var _ = Steam.Instance.Apps.PICSGetProductInfo( null, appId, false, false );
            }
            else
            {
                // send off a product request as well so we get something to cache for later
                var _ = Steam.Instance.Apps.PICSGetProductInfo( appId, null, false, false );
            }

            var result = await playerCountHandler.RequestPlayerCountWeb( appId );

            // var result = await playerCountHandler.RequestPlayerCount( appId );

            OnNumPlayers( result, new Request { AppID = appId, Channel = details.Channel, Requester = details.Sender } );
        }

        void OnNumPlayers( PlayerCountHandler.PlayerCountDetails details, Request req )
        {
            if ( details.Result != EResult.OK )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request player counts for {1}: {2}", req.Requester.Nickname, GetAppName( req.AppID ), details.Result );
                return;
            }

            IRC.Instance.Send( req.Channel, "{0}: {1} players: {2}", req.Requester.Nickname, GetAppName( req.AppID ), details.Count );
        }


        void OnNumPlayers( SteamUserStats.NumberOfPlayersCallback callback )
        {
            var req = GetRequest( r => r.JobID == callback.JobID );

            if ( req == null )
                return;

            OnNumPlayers( new PlayerCountHandler.PlayerCountDetails { AppID = req.AppID, Result = callback.Result, Count = callback.NumPlayers }, req );

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
