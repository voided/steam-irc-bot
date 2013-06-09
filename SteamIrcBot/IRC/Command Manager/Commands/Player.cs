using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;
using SteamKit2.Unified.Internal;

namespace SteamIrcBot
{
    class OwnedGamesCommand : Command<OwnedGamesCommand.Request>
    {
        public class Request : BaseRequest
        {
            public JobID Job { get; set; }

            public SteamID SteamID { get; set; }
        }

        public OwnedGamesCommand()
        {
            Trigger = "!ownedgames";
            HelpText = "!ownedgames <steamid> - Displays the number of owned games of a given SteamID";

            new JobCallback<SteamUnifiedMessages.ServiceMethodResponse>( OnServiceMethod, Steam.Instance.CallbackManager );
        }


        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: SteamID argument required", details.Sender.Nickname );
                return;
            }

            var inputId = details.Args[ 0 ];
            SteamID steamId;

            if ( !SteamUtils.TryDeduceSteamID( inputId, out steamId ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to deduce SteamID from given input", details.Sender.Nickname );
                return;
            }

            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to request owned games: not connected to Steam!", details.Sender.Nickname );
                return;
            }

            var request = new CPlayer_GetOwnedGames_Request();
            request.steamid = steamId;
            request.include_played_free_games = true;

            JobID job = Steam.Instance.Unified.SendMessage( "Player.GetOwnedGames#1", request );
            AddRequest( details, new Request { Job = job, SteamID = steamId } );
        }


        void OnServiceMethod( SteamUnifiedMessages.ServiceMethodResponse callback, JobID jobId )
        {
            var req = GetRequest( r => r.Job == jobId );

            if ( req == null )
                return;

            if ( callback.Result != EResult.OK )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to make service call: {1}", req.Requester.Nickname, callback.Result );
                return;
            }

            var response = callback.GetDeserializedResponse<CPlayer_GetOwnedGames_Response>();

            IRC.Instance.Send( req.Channel, "{0}: {1} owns {2} games", req.Requester.Nickname, req.SteamID, response.game_count );
        }
    }

    class PlayedGamesCommand : Command<PlayedGamesCommand.Request>
    {
        public class Request : BaseRequest
        {
            public JobID Job { get; set; }

            public SteamID SteamID { get; set; }
        }


        public PlayedGamesCommand()
        {
            Trigger = "!playedgames";
            HelpText = "!playedgames <steamid> - Displays info about recently played games of a given SteamID";

            new JobCallback<SteamUnifiedMessages.ServiceMethodResponse>( OnServiceMethod, Steam.Instance.CallbackManager );
        }


        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: SteamID argument required", details.Sender.Nickname );
                return;
            }

            var inputId = details.Args[ 0 ];
            SteamID steamId;

            if ( !SteamUtils.TryDeduceSteamID( inputId, out steamId ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to deduce SteamID from given input", details.Sender.Nickname );
                return;
            }

            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to request recently played games: not connected to Steam!", details.Sender.Nickname );
                return;
            }

            var request = new CPlayer_GetRecentlyPlayedGames_Request();
            request.steamid = steamId;

            JobID job = Steam.Instance.Unified.SendMessage( "Player.GetRecentlyPlayedGames#1", request );
            AddRequest( details, new Request { Job = job, SteamID = steamId } );
        }


        void OnServiceMethod( SteamUnifiedMessages.ServiceMethodResponse callback, JobID jobId )
        {
            var req = GetRequest( r => r.Job == jobId );

            if ( req == null )
                return;

            if ( callback.Result != EResult.OK )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to make service call: {1}", req.Requester.Nickname, callback.Result );
                return;
            }

            var response = callback.GetDeserializedResponse<CPlayer_GetRecentlyPlayedGames_Response>();

            var gameStrings = response.games
                .Take( 5 ) // max 5 games
                .Select( g => string.Format( "{0} ({1}): {2}", g.name, g.appid, GetPlaytimeString( g.playtime_forever ) ) );

            IRC.Instance.Send( req.Channel, "{0}: Played games for {1}: {2}", req.Requester.Nickname, req.SteamID, string.Join( ", ", gameStrings ) );
        }


        string GetPlaytimeString( int minutes )
        {
            string playTime = "";

            if ( minutes > 60 )
            {
                playTime = string.Format( "{0}hrs", minutes / 60 );
                minutes %= 60;
            }

            playTime += string.Format( "{0}mins", minutes );

            return playTime;
        }
    }

    class BadgesCommand : Command<BadgesCommand.Request>
    {
        public class Request : BaseRequest
        {
            public JobID Job { get; set; }

            public SteamID SteamID { get; set; }
        }


        public BadgesCommand()
        {
            Trigger = "!badges";
            HelpText = "!badges <steamid> - Displays info about badges and XP of a given SteamID";

            new JobCallback<SteamUnifiedMessages.ServiceMethodResponse>( OnServiceMethod, Steam.Instance.CallbackManager );
        }


        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: SteamID argument required", details.Sender.Nickname );
                return;
            }

            var inputId = details.Args[ 0 ];
            SteamID steamId;

            if ( !SteamUtils.TryDeduceSteamID( inputId, out steamId ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to deduce SteamID from given input", details.Sender.Nickname );
                return;
            }

            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to request badges: not connected to Steam!", details.Sender.Nickname );
                return;
            }

            var request = new CPlayer_GetRecentlyPlayedGames_Request();
            request.steamid = steamId;

            JobID job = Steam.Instance.Unified.SendMessage( "Player.GetBadges#1", request );
            AddRequest( details, new Request { Job = job, SteamID = steamId } );
        }


        void OnServiceMethod( SteamUnifiedMessages.ServiceMethodResponse callback, JobID jobId )
        {
            var req = GetRequest( r => r.Job == jobId );

            if ( req == null )
                return;

            if ( callback.Result != EResult.OK )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to make service call: {1}", req.Requester.Nickname, callback.Result );
                return;
            }

            var response = callback.GetDeserializedResponse<CPlayer_GetBadges_Response>();

            IRC.Instance.Send( req.Channel, "{0}: {1} is level {2} with {3} badges and {4} XP, {5} more XP to next level",
                req.Requester.Nickname, req.SteamID, response.player_level, response.badges.Count, response.player_xp, response.player_xp_needed_to_level_up );
        }
    }
}
