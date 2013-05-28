using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using SteamKit2;

namespace SteamIrcBot
{
    class SteamHandler
    {
        public SteamHandler( CallbackManager manager )
        {
        }
    }

    class SteamManager
    {
        List<SteamHandler> handlers;

        public SteamManager( CallbackManager manager )
        {
            handlers = new List<SteamHandler>();

            var handlerTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where( t => t.IsSubclassOf( typeof( SteamHandler ) ) );

            foreach ( var type in handlerTypes )
            {
                var handler = Activator.CreateInstance( type, manager ) as SteamHandler;

                Log.WriteDebug( "SteamManager", "Registering handler {0}", type );

                handlers.Add( handler );
            }
        }
    }
}
