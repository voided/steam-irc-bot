using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamIrcBot
{
    class SIDCommand : Command<SIDCommand.Request>
    {
        public class Request : BaseRequest
        {
            public SteamID SteamID { get; set; }
        }


        public SIDCommand()
        {
            new Callback<SteamFriends.PersonaStateCallback>( OnPersonaState, Steam.Instance.CallbackManager );

            Triggers.Add( "!sid" );
            Triggers.Add( "!steamid" );
            Triggers.Add( "!steam" );
            HelpText = "!sid <steamid> - Displays info about the given SteamID, and requests persona/clan name for relevant types";
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

            IRC.Instance.Send( details.Channel, "{0}: {1}", details.Sender.Nickname, SteamUtils.ExpandSteamID( steamId ) );

            if ( !Steam.Instance.Connected )
            {
                IRC.Instance.Send( details.Channel, details.Sender.Nickname + ": Unable to request persona info: not connected to Steam!" );
                return;
            }

            if ( steamId.IsIndividualAccount || steamId.IsClanAccount )
            {
                AddRequest( details, new Request { SteamID = steamId } );
                Steam.Instance.Friends.RequestFriendInfo( steamId, ( EClientPersonaStateFlag )short.MaxValue );
            }
        }

        void OnPersonaState( SteamFriends.PersonaStateCallback callback )
        {
            var req = GetRequest( r => r.SteamID == callback.FriendID );

            if ( req == null )
                return; // not a sid we requested

            if ( req.SteamID.IsClanAccount )
            {
                IRC.Instance.Send( req.Channel, "{0}: {1} (http://steamcommunity.com/gid/{2}/)",
                    req.Requester.Nickname, callback.Name, req.SteamID.ConvertToUInt64()
                );
            }
            else if ( req.SteamID.IsIndividualAccount )
            {
                IRC.Instance.Send( req.Channel, "{0}: {1} (http://steamcommunity.com/profiles/{2}/) (Last Online = {3}, Last Offline = {4})",
                    req.Requester.Nickname, callback.Name, req.SteamID.ConvertToUInt64(), callback.LastLogOn, callback.LastLogOff
                );
            }
            else
            {
                IRC.Instance.Send( req.Channel, "{0}: {1}", req.Requester.Nickname, callback.Name );
            }
        }
    }

    class ProfileCommand : Command<ProfileCommand.Request>
    {
        public class Request : BaseRequest
        {
            public JobID JobID { get; set; }
            public SteamID SteamID { get; set; }
        }


        public ProfileCommand()
        {
            new JobCallback<SteamFriends.ProfileInfoCallback>( OnProfileInfo, Steam.Instance.CallbackManager );

            Triggers.Add( "!profile" );
            Triggers.Add( "!steamprofile" );
            HelpText = "!profile <steamid> - Request profile details about a given user";
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
                IRC.Instance.Send( details.Channel, "{0}: Unable to request profile info: not connected to Steam!", details.Sender.Nickname );
                return;
            }

            if ( steamId.IsIndividualAccount )
            {
                var jobId = Steam.Instance.Friends.RequestProfileInfo( steamId );
                AddRequest( details, new Request { JobID = jobId, SteamID = steamId } );
            }
            else
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to request profile info: Not an individual account!", details.Sender.Nickname );
            }
        }

        void OnProfileInfo( SteamFriends.ProfileInfoCallback callback, JobID jobId )
        {
            var req = GetRequest( r => r.JobID == jobId );

            if ( req == null )
                return;

            if ( callback.Result != EResult.OK )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request profile info for {1}: {2}", req.Requester.Nickname, req.SteamID, callback.Result );
                return;
            }

            var displayDict = new DisplayDictionary();

            displayDict.Add( "Real Name", callback.RealName );
            displayDict.Add( "Headline", callback.Headline );
            displayDict.Add( "City", callback.CityName );
            displayDict.Add( "State", callback.StateName );
            displayDict.Add( "Country", callback.CountryName );
            displayDict.Add( "Time Created", callback.TimeCreated );

            displayDict.Add( "Summary", callback.Summary );

            IRC.Instance.Send( req.Channel, "{0}: {1}: {2}", req.Requester.Nickname, req.SteamID, displayDict );
            IRC.Instance.Send( req.Channel, "{0}: http://steamcommunity.com/profiles/{1}/", req.Requester.Nickname, req.SteamID.ConvertToUInt64() );
        }

    }
}
