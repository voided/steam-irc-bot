using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamIrcBot
{
    class RehashCommand : Command
    {
        DateTime lastRehash = DateTime.Now;

        public RehashCommand()
        {
            Triggers.Add( "!rehash" );
            HelpText = "!rehash - Reloads bot settings";
        }

        protected override void OnRun( CommandDetails details )
        {
            TimeSpan timeDiff = DateTime.Now - lastRehash;

            if ( timeDiff <= TimeSpan.FromSeconds( 5 ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Cannot rehash yet", details.Sender.Nickname );
                return;
            }

            lastRehash = DateTime.Now;

            try
            {
                Settings.Load();
            }
            catch ( Exception ex )
            {
                IRC.Instance.Send( details.Channel, "{0}: Unable to load settings. Error has been logged", details.Sender.Nickname );
                Log.WriteError( "RehashCommand", "Unable to rehash settings: {0}", ex.Message );
                return;
            }

            if ( !Settings.Validate() )
            {
                IRC.Instance.Send( details.Channel, "{0}: Settings did not validate. Error has been logged", details.Sender.Nickname );
                return;
            }

            IRC.Instance.Send( details.Channel, "{0}: Rehashed", details.Sender.Nickname );

            // rejoin any channels we may have edited
            IRC.Instance.Join( Settings.Current.Channels.Select( chan => chan.Channel ).ToArray() );
        }
    }
}
