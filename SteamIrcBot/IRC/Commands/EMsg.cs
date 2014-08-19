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
            EMsg eMsg;

            if ( Enum.TryParse( inputEMsg, out eMsg ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: {1}", details.Sender.Nickname, eMsg );
            }
            else
            {
                bool includeDeprecated = false;
                if ( details.Args.Length > 1 && details.Args[ 1 ].Equals( "deprecated", StringComparison.InvariantCultureIgnoreCase ) )
                {
                    includeDeprecated = true;
                }

                var emsgs = Enum.GetValues( typeof( EMsg ) ).Cast<EMsg>();
                if ( !includeDeprecated )
                {
                    emsgs = emsgs.Except( emsgs.Where( x => typeof( EMsg ).GetMember( x.ToString() )[ 0 ].GetCustomAttributes( typeof( ObsoleteAttribute ), inherit: false ).Any() ) );
                }

                var emsgsWithMatchingName = emsgs.Where( x => x.ToString().IndexOf( inputEMsg, StringComparison.InvariantCultureIgnoreCase ) >= 0 );
                if ( emsgsWithMatchingName.Count() == 0 )
                {
                    IRC.Instance.Send( details.Channel, "{0}: No matches found.", details.Sender.Nickname );
                }
                else if ( emsgsWithMatchingName.Count() > 10 )
                {
                    IRC.Instance.Send( details.Channel, "{0}: More than 10 results found.", details.Sender.Nickname );
                }
                else
                {
                    var formatted = string.Join( ", ", emsgsWithMatchingName.Select( emsg => string.Format( "{0} ({1})", emsg.ToString(), ( int )emsg ) ) );
                    IRC.Instance.Send( details.Channel, "{0}: {1}", details.Sender.Nickname, formatted );
                }
            }
        }
    }
}
