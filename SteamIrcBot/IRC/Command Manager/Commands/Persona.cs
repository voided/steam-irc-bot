using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

            Trigger = "!sid";
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


        const string CLAN_URL = "http://steamcommunity.com/gid/{0}/";
        const string INDIVIDUAL_URL = "http://steamcommunity.com/profiles/{0}/";

        void OnPersonaState( SteamFriends.PersonaStateCallback callback )
        {
            var req = GetRequest( r => r.SteamID == callback.FriendID );

            if ( req == null )
                return; // not a sid we requested

            string url = null;

            if ( req.SteamID.IsClanAccount )
            {
                url = CLAN_URL;
            }
            else if ( req.SteamID.IsIndividualAccount )
            {
                url = INDIVIDUAL_URL;
            }

            if ( url != null )
            {
                IRC.Instance.Send( req.Channel, "{0}: {1} ({2}) (Last Online = {3}, Last Offline = {4})",
                    req.Requester.Nickname, callback.Name, string.Format( url, req.SteamID.ConvertToUInt64() ), callback.LastLogOn, callback.LastLogOff );
            }
            else
            {
                IRC.Instance.Send( req.Channel, "{0}: {1}", req.Requester.Nickname, callback.Name );
            }

        }
    }
}
