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
        List<Command> registeredCommands = new List<Command>();


        public CommandManager( IrcClient client )
        {
            client.MessageParser.ChannelMessage += MessageParser_ChannelMessage;

            var commandTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where( t => !t.IsAbstract )
                .Where( t => t.IsSubclassOf( typeof( Command ) ) );

            foreach ( var command in commandTypes )
            {
                var cmd = Activator.CreateInstance( command ) as Command;
                registeredCommands.Add( cmd );
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

            var triggeredCommand = registeredCommands
                .FirstOrDefault( c => string.Equals( command, c.Trigger, StringComparison.OrdinalIgnoreCase ) );

            if ( triggeredCommand == null )
                return;

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
