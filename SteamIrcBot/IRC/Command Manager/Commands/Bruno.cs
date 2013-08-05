using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamIrcBot
{
    class BrunoCommand : Command
    {
        public BrunoCommand()
        {
            Triggers.Add( "!bruno" );
            HelpText = "!bruno - Acquire vast sums of knowledge through stats, one quote at a time";
        }

        protected override void OnRun( CommandDetails details )
        {
            var rand = new Random();

            string quote = Settings.Current.BrunoQuotes[ rand.Next( Settings.Current.BrunoQuotes.Count ) ];

            IRC.Instance.Send( details.Channel, "\"{0}\" - Bruno", quote );
        }
    }
}
