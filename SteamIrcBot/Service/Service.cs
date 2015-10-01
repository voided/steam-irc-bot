using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace SteamIrcBot
{
    public partial class BotService : ServiceBase
    {
        public BotService()
        {
            InitializeComponent();
        }


        public void Start( string[] args )
        {
            OnStart( args );
        }


        protected override void OnStart( string[] args )
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

            RSS.Instance.Start();

            IRC.Instance.Connect();
        }

        protected override void OnStop()
        {
            Steam.Instance.Disconnect();

            IRC.Instance.Disconnect();

            RSS.Instance.Stop();

            ServiceDispatcher.Instance.Stop();
        }
    }
}
