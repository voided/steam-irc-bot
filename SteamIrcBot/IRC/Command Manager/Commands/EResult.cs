using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamIrcBot
{
    class EResultCommand : Command
    {
        public EResultCommand()
        {
            Triggers.Add( "!eresult" );
            HelpText = "!eresult <eresult> - Returns the EResult string for a given value";
        }

        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: EResult argument required", details.Sender.Nickname );
                return;
            }

            string inputEResult = details.Args[ 0 ];
            int eResult;

            if ( !int.TryParse( inputEResult, out eResult ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid EResult value", details.Sender.Nickname );
                return;
            }

            IRC.Instance.Send( details.Channel, "{0}: {1}", details.Sender.Nickname, ( EResult )eResult );
        }
    }
}
