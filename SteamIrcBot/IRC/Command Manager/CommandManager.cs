using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Meebey.SmartIrc4net;

namespace SteamIrcBot
{
    class CommandManager
    {
        public List<Command> RegisteredCommands { get; private set; }
        object commandLock = new object();


        public CommandManager( IrcClient client )
        {
            RegisteredCommands = new List<Command>();

            client.OnChannelMessage += MessageParser_ChannelMessage;
        }

        public void Init()
        {
            var commandTypes =
                Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where( t => !t.IsAbstract )
                .Where( t => t.IsSubclassOf( typeof( Command ) ) );

            lock ( commandLock )
            {
                // Init and Tick are called on different threads during startup
                // so we need to serialize access to our command list during initialization

                foreach ( var command in commandTypes )
                {
                    var cmd = Activator.CreateInstance( command ) as Command;

                    Log.WriteDebug( "CommandManager", "Registering command {0}: {1}", command.Name, cmd.Triggers.First() );

                    RegisteredCommands.Add( cmd );
                }
            }
        }

        public void Tick()
        {
            var expirableCommands = RegisteredCommands
                .Where( c => c.GetType().Implements( typeof( IRequestableCommand ) ) )
                .Cast<IRequestableCommand>();

            lock ( commandLock )
            {
                foreach ( var cmd in expirableCommands )
                {
                    cmd.ExpireRequests();
                }
            }
        }

        void MessageParser_ChannelMessage( object sender, IrcEventArgs e )
        {
            string msg = e.Data.Message;

            if ( string.IsNullOrEmpty( msg ) )
                return;

            string[] splits = msg.Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries );

            if ( splits.Length == 0 )
                return;

            string command = splits[ 0 ];
            string[] args = splits.Skip( 1 ).ToArray();

            var from = new SenderDetails
            {
                Nickname = e.Data.Nick,
                Ident = e.Data.Ident,
                Hostname = e.Data.Host,
            };

            // dumb skype relay related hack
            if ( from.Ident == "~gib" && ( from.Hostname == "me.the.steamgames.co" || from.Hostname == "2001:470:1f0f:4eb::4:1" ) )
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
                from.Nickname = senderNick;
            }

            var triggeredCommand = RegisteredCommands
                .FirstOrDefault( c => c.Triggers.Any( t => string.Equals( command, t, StringComparison.OrdinalIgnoreCase ) ) );

            if ( triggeredCommand == null )
                return;

            Log.WriteInfo( "CommandManager", "Handling command {0} from {1} in {2}", triggeredCommand.Triggers.First(), from, e.Data.Channel );

            triggeredCommand.DoRun( new CommandDetails
            {
                Trigger = command,
                Args = args,

                Sender = from,
                Channel = e.Data.Channel,
            } );
        }
    }
}
