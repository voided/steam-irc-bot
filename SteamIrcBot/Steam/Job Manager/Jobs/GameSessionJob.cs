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

            Log.WriteDebug( "GameSessionJob", "Updating game session" );

            Steam.Instance.Games.PlayGame( 440 );
        }
    }
}
