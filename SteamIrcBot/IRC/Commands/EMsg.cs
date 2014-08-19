using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamIrcBot
{
    class EMsgCommand : Command
    {
        public EMsgCommand()
        {
            Triggers.Add( "!emsg" );
            HelpText = "!emsg <emsg> - Returns the EMsg string for a given value";
        }

        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: EMsg argument required", details.Sender.Nickname );
                return;
            }

            string inputEMsg = details.Args[ 0 ];
            int eMsg;

            if ( !int.TryParse( inputEMsg, out eMsg ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid EMsg value", details.Sender.Nickname );
                return;
            }

            IRC.Instance.Send( details.Channel, "{0}: {1}", details.Sender.Nickname, ( EMsg )eMsg );
        }
    }
}
