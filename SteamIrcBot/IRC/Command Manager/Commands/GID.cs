using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamIrcBot
{
    class GIDCommand : Command
    {
        public GIDCommand()
        {
            Trigger = "!gid";
            HelpText = "!gid - decompose a Steam GID_t";
        }

        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length == 0 )
            {
                IRC.Instance.Send( details.Channel, "{0}: SteamID argument required", details.Sender.Nickname );
                return;
            }

            var inputGid = details.Args[ 0 ];

            ulong ulGid;
            if ( !ulong.TryParse( inputGid, out ulGid ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid GID", details.Sender.Nickname );
                return;
            }

            GID gid = ulGid;

            IRC.Instance.Send( details.Channel, "{0}: {1}", details.Sender.Nickname, SteamUtils.ExpandGID( gid ) );
        }
    }
}
