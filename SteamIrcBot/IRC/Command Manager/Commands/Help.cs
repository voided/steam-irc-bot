using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamIrcBot
{
    class HelpCommand : Command
    {
        public HelpCommand()
        {
            Trigger = "!help";
            HelpText = "!help <command> - Displays a list of commands or info about a specific command";
        }


        protected override void OnRun( CommandDetails details )
        {
            var commands = IRC.Instance.CommandManager.RegisteredCommands;

            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: Available commands: {1}", details.Sender.Nickname, string.Join( ", ", commands.Select( c => c.Trigger ) ) );
                return;
            }

            var cmd = details.Args[ 0 ];

            var foundCommands = commands
                .Where( c => c.Trigger.IndexOf( cmd, StringComparison.OrdinalIgnoreCase ) != -1 )
                .ToList();

            if ( foundCommands.Count == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to find command with text '{1}'", details.Sender.Nickname, cmd );
            }
            else if ( foundCommands.Count == 1 )
            {
                IRC.Instance.Send( details.Channel, "{0}: {1}", details.Sender.Nickname, foundCommands[ 0 ].HelpText );
            }
            else
            {
                IRC.Instance.Send( details.Channel, "{0}: Found multiple commands: {1}", details.Sender.Nickname, string.Join( ", ", foundCommands.Select( c => c.Trigger ) ) );
            }
        }
    }
}
