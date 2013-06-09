using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;
using SteamKit2.Unified.Internal;

namespace SteamIrcBot
{
    class LevelDistributionCommand : Command<LevelDistributionCommand.Request>
    {
        public class Request : BaseRequest
        {
            public JobID Job { get; set; }

            public uint Level { get; set; }
        }

        public LevelDistributionCommand()
        {
            Trigger = "!leveldist";
            HelpText = "!leveldist <level> - Displays the Steam level distribution for a given level";

            new JobCallback<SteamUnifiedMessages.ServiceMethodResponse>( OnServiceMethod, Steam.Instance.CallbackManager );
        }

        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: Steam level argument required", details.Sender.Nickname );
                return;
            }

            var inputLevel = details.Args[ 0 ];
            uint level;

            if ( !uint.TryParse( inputLevel, out level ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid Steam level value", details.Sender.Nickname );
                return;
            }

            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to request Steam level distribution: not connected to Steam!", details.Sender.Nickname );
                return;
            }

            var request = new CPlayer_GetSteamLevelDistribution_Request();
            request.player_level = level;

            JobID job = Steam.Instance.Unified.SendMessage( "Player.GetSteamLevelDistribution#1", request );
            AddRequest( details, new Request { Job = job, Level = level } );
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

            var response = callback.GetDeserializedResponse<CPlayer_GetSteamLevelDistribution_Response>();

            IRC.Instance.Send( req.Channel, "{0}: Steam level {1}: {2}% percentile, rank {3} of 100", req.Requester.Nickname, req.Level, response.player_level_percentile, response.top_100_ranking );
        }
    }
    class SteamLevelCommand : Command<SteamLevelCommand.Request>
    {
        public class Request : BaseRequest
        {
            public JobID Job { get; set; }

            public SteamID SteamID { get; set; }
        }


        public SteamLevelCommand()
        {
            Trigger = "!level";
            HelpText = "!level <steamid> - Displays the Steam level of a given SteamID";

            new JobCallback<SteamLevels.SteamLevelsCallback>( OnLevels, Steam.Instance.CallbackManager );
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
                IRC.Instance.Send( details.Channel, details.Sender.Nickname + ": Unable to request steam level: not connected to Steam!" );
                return;
            }

            JobID job = Steam.Instance.Levels.RequestLevels( new SteamID[] { steamId } );
            AddRequest( details, new Request { Job = job, SteamID = steamId } );
        }


        void OnLevels( SteamLevels.SteamLevelsCallback callback, JobID jobId )
        {
            var req = GetRequest( r => r.Job == jobId );

            if ( req == null )
                return;

            var friend = callback.Friends
                .SingleOrDefault( f => f.FriendID == req.SteamID );

            if ( friend == null )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request Steam level for {1}!", req.Requester.Nickname, req.SteamID );
                return;
            }

            IRC.Instance.Send( req.Channel, "{0}: Steam level for {1}: {2}", req.Requester.Nickname, req.SteamID, friend.Level );
        }
    }
}
