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

                Stop();
                return;
            }

            if ( !Settings.Validate() )
            {
                Stop();
                return;
            }

            IRC.Instance.Connect();

            IRC.Instance.JoinEvent.WaitOne( TimeSpan.FromMinutes( 1 ) );

            CallbackDispatcher.Instance.Start();
            Steam.Instance.Connect();
        }

        protected override void OnStop()
        {
            Steam.Instance.Disconnect();
            IRC.Instance.Disconnect();

            CallbackDispatcher.Instance.Stop();
        }
    }
}
