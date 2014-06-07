using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamIrcBot
{
    class GameSessionJob : Job
    {
        public GameSessionJob( CallbackManager manager )
        {
            Period = TimeSpan.FromMinutes( 5 );
        }

        protected override void OnRun()
        {
            if ( !Steam.Instance.Connected )
                return;

            if ( Settings.Current.GCApps.Count > 0 )
            {
                Steam.Instance.Games.PlayGames( Settings.Current.GCApps.Select( app => app.AppID ) );
            }
        }
    }
}
