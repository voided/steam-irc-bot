using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Reflection;
using System.IO;

namespace SteamIrcBot
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main( string[] args )
        {
            string path = Assembly.GetExecutingAssembly().Location;
            path = Path.GetDirectoryName( path );
            Directory.SetCurrentDirectory( path );

            AppDomain.CurrentDomain.UnhandledException += ( sender, e ) =>
            {
                Log.WriteError( "Program", "Unhandled exception (IsTerm: {0}): {1}", e.IsTerminating, e.ExceptionObject );
            };

            var service = new BotService();

#if SERVICE_BUILD
            ServiceBase.Run( service );
#else 
            service.Start( args );
            ServiceDispatcher.Instance.Wait();
#endif
        }
    }
}
