using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;
using SteamKit2.Unified.Internal;

namespace SteamIrcBot
{
    class SteamLevelCommand : Command<SteamLevelCommand.Request>
    {
        public class Request : BaseRequest
        {
            public JobID Job { get; set; }

            public SteamID SteamID { get; set; }
        }


        public SteamLevelCommand()
        {
            Triggers.Add( "!level" );
            Triggers.Add( "!steamlevel" );
            HelpText = "!level <steamid> - Displays the Steam level of a given SteamID";

            Steam.Instance.CallbackManager.Subscribe<SteamLevels.SteamLevelsCallback>( OnLevels );
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


        void OnLevels( SteamLevels.SteamLevelsCallback callback )
        {
            var req = GetRequest( r => r.Job == callback.JobID );

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
