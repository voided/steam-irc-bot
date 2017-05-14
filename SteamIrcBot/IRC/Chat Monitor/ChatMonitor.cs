using Meebey.SmartIrc4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SteamIrcBot
{
    class ChatMonitor
    {
        private readonly object monitorsLock = new object();

        private readonly List<BaseMonitor> registeredMonitors;

        public ChatMonitor( IrcClient client )
        {
            client.OnChannelMessage += Client_OnChannelMessage;

            registeredMonitors = new List<BaseMonitor>();
        }


        public void Init()
        {
            var monitors =
                Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where( t => !t.IsAbstract )
                .Where( t => t.IsSubclassOf( typeof( BaseMonitor ) ) );

            lock ( monitorsLock )
            {
                // Init and Tick are called on different threads during startup
                // so we need to serialize access to our monitor list during initialization

                foreach ( var monitor in monitors )
                {
                    var monitorInstance = Activator.CreateInstance( monitor ) as BaseMonitor;

                    Log.WriteDebug( "ChatMonitor", "Registering monitor {0}", monitor.Name );

                    registeredMonitors.Add( monitorInstance );
                }
            }
        }

        private void Client_OnChannelMessage( object sender, IrcEventArgs e )
        {
            if ( string.IsNullOrEmpty( e.Data.Message ) )
                return;

            var from = new SenderDetails
            {
                Nickname = e.Data.Nick,
                Ident = e.Data.Ident,
                Hostname = e.Data.Host,
            };

            var messageDetails = new MessageDetails( e.Data.Channel, from, e.Data.Message );

            lock ( monitorsLock )
            {
                foreach ( var monitor in registeredMonitors )
                {
                    monitor.DoMessage( messageDetails );
                }
            }
        }
    }
}
