using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using SteamKit2;

namespace SteamIrcBot
{
    abstract class SteamHandler
    {
        public SteamHandler( CallbackManager manager )
        {
        }

        public abstract void Tick();
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


        public void Tick()
        {
            foreach ( var handlr in handlers )
            {
                handlr.Tick();
            }
        }


        public T GetHandler<T>() where T : SteamHandler
        {
            foreach ( var hndlr in handlers )
            {
                if ( hndlr.GetType() == typeof( T ) )
                {
                    return hndlr as T;
                }
            }

            return null;
        }
    }
}
