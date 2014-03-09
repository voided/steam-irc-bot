using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamIrcBot
{
    class MakeSIDCommand : Command
    {

        public MakeSIDCommand()
        {
            Triggers.Add( "!makesid" );
            Triggers.Add( "!makesteamid" );
            HelpText = "!makesid <universe> <type> <instance> <id> - Crafts a SteamID with the given parameters, <id> can be a resolvable SteamID";
        }


        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length < 4 )
            {
                IRC.Instance.Send( details.Channel, "{0}: Usage: !makesid <universe> <type> <instance> <id>", details.Sender.Nickname );
                return;
            }

            string univ = details.Args[ 0 ];
            string type = details.Args[ 1 ];
            string instance = details.Args[ 2 ];
            string id = details.Args[ 3 ];

            EUniverse eUniv;
            if ( !Enum.TryParse( univ, true, out eUniv ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid universe, should be one of: {1}",
                    details.Sender.Nickname, string.Join( ", ", Enum.GetNames( typeof( EUniverse ) ) )
                );

                return;
            }

            EAccountType eType;
            if ( !Enum.TryParse( type, true, out eType ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid account type, should be one of: {1}",
                    details.Sender.Nickname, string.Join( ", ", Enum.GetNames( typeof( EAccountType ) ) )
                );

                return;
            }

            uint uiInstance = 0;
            switch ( instance.ToLower() )
            {
                case "desktop":
                    uiInstance = SteamID.DesktopInstance;
                    break;

                case "console":
                    uiInstance = SteamID.ConsoleInstance;
                    break;

                case "web":
                    uiInstance = SteamID.WebInstance;
                    break;

                case "all":
                    uiInstance = SteamID.AllInstances;
                    break;

                default:
                    SteamID.ChatInstanceFlags instanceFlags;
                    if ( Enum.TryParse( instance, true, out instanceFlags ) )
                    {
                        uiInstance = ( uint )instanceFlags;
                        break;
                    }

                    if ( !uint.TryParse( instance, out uiInstance ) )
                    {
                        IRC.Instance.Send( details.Channel, "{0}: Invalid instance! (desktop, console, web, all, clan, lobby, mmslobby, #)", details.Sender.Nickname );
                        return;
                    }
                    break;
            }

            uint accountId;
            if ( !uint.TryParse( id, out accountId ) )
            {
                SteamID steamId;
                if ( !SteamUtils.TryDeduceSteamID( id, out steamId ) )
                {
                    IRC.Instance.Send( details.Channel, "{0}: Invalid (or undeducible) account id!", details.Sender.Nickname );
                    return;
                }

                accountId = steamId.AccountID;
            }

            SteamID steamID = new SteamID( accountId, uiInstance, eUniv, eType );

            var sidCommand = IRC.Instance.CommandManager.RegisteredCommands
                .SingleOrDefault( c => c is SIDCommand );

            if ( sidCommand != null )
            {
                // loading up on the dirty hacks
                details.Args = new string[] { steamID.ConvertToUInt64().ToString() };
                sidCommand.DoRun( details );
            }
            else
            {
                IRC.Instance.Send( details.Channel, "{0}: An internal error has occurred", details.Sender.Nickname );
                Log.WriteError( "MakeSIDCommand", "Unable to find SIDCommand!" );
            }
        }
    }
}
