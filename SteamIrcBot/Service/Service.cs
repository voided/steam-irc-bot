using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SteamIrcBot
{
    public class BotService
    {
        public void Start( string[] args )
        {
            OnStart( args );
        }

        public void Stop()
        {
            OnStop();
        }


        protected void OnStart( string[] args )
        {
            try
            {
                Settings.Load();
            }
            catch ( Exception ex )
            {
                Log.WriteError( "BotService", "Unable to load settings: {0}", ex );

                Environment.Exit( 1 );
                return;
            }

            if ( !Settings.Validate() )
            {
                Environment.Exit( 1 );
                return;
            }

            Steam.Instance.Init();

            ServiceDispatcher.Instance.Start();

            RSSService.Instance.Start();

            IRC.Instance.Connect();
        }

        protected void OnStop()
        {
            Steam.Instance.Disconnect();

            IRC.Instance.Disconnect();

            RSSService.Instance.Stop();

            ServiceDispatcher.Instance.Stop();
        }
    }
}
