using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HeronIRC;
using System.Reflection;

namespace SteamIrcBot
{
    class CommandManager
    {
        public List<Command> RegisteredCommands { get; private set; }


        public CommandManager( IrcClient client )
        {
            RegisteredCommands = new List<Command>();

            client.MessageParser.ChannelMessage += MessageParser_ChannelMessage;

            var commandTypes =
                Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where( t => !t.IsAbstract )
                .Where( t => t.IsSubclassOf( typeof( Command ) ) );

            foreach ( var command in commandTypes )
            {
                var cmd = Activator.CreateInstance( command ) as Command;

                Log.WriteDebug( "CommandManager", "Registering command {0}: {1}", command.Name, cmd.Trigger );

                RegisteredCommands.Add( cmd );
            }
        }

        public void ExpireRequests()
        {
            var expirableCommands = RegisteredCommands
                .Where( c => c.GetType().Implements( typeof( IRequestableCommand ) ) )
                .Cast<IRequestableCommand>();

            foreach ( var cmd in expirableCommands )
            {
                cmd.ExpireRequests();
            }
        }

        void MessageParser_ChannelMessage( object sender, MessageEventArgs e )
        {
            if ( string.IsNullOrEmpty( e.Message ) )
                return;

            string[] splits = e.Message.Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries );

            if ( splits.Length == 0 )
                return;

            string command = splits[ 0 ];
            string[] args = splits.Skip( 1 ).ToArray();

            // dumb skype relay related hack
            if ( e.Sender.Ident == "~gib" && e.Sender.Hostname == "me.the.steamgames.co" )
            {
                command = splits.Skip( 1 )
                    .FirstOrDefault();

                args = splits
                    .Skip( 2 )
                    .ToArray();

                if ( string.IsNullOrEmpty( command ) )
                    return;

                var senderNick = splits[ 0 ];

                if ( senderNick.StartsWith( "<" ) && senderNick.EndsWith( ">" ) )
                {
                    senderNick = senderNick.Substring( 1, senderNick.Length - 2 );
                }

                // turbo dirty
                e.Sender.Nickname = senderNick;
            }

            var triggeredCommand = RegisteredCommands
                .FirstOrDefault( c => string.Equals( command, c.Trigger, StringComparison.OrdinalIgnoreCase ) );

            if ( triggeredCommand == null )
                return;

            Log.WriteInfo( "CommandManager", "Handling command {0} from {1} in {2}", triggeredCommand.Trigger, e.Sender, e.SourceChannel );

            triggeredCommand.DoRun( new CommandDetails
            {
                Trigger = command,
                Args = args,

                Sender = e.Sender,
                Channel = e.SourceChannel
            } );
        }
    }
}
