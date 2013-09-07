using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamIrcBot
{
    class BugCommand : Command
    {
        public BugCommand()
        {
            Triggers.Add( "!bug" );
            HelpText = "!bug <bugid> - Link to a AM bug";
        }

        protected override void OnRun( CommandDetails details )
        {
            if ( IRC.Instance.IsUserOnChannel( details.Channel, "yakbot" ) )
                return; // glory to the yak

            if ( details.Args.Length < 1 )
            {
                IRC.Instance.Send( details.Channel, "{0}: {1}", details.Sender.Nickname, HelpText );
                return;
            }

            int bugId;
            if ( !int.TryParse( details.Args[ 0 ], out bugId ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid bug ID", details.Sender.Nickname );
                return;
            }

            IRC.Instance.Send( details.Channel, "{0}: https://bugs.alliedmods.net/show_bug.cgi?id={1}", details.Sender.Nickname, bugId );
        }
    }

    class MozBugCommand : Command
    {
        public MozBugCommand()
        {
            Triggers.Add( "!mozbug" );
            Triggers.Add( "!mug" );
            HelpText = "!mozbug <bugid> - Link to a mozilla bug";
        }

        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length < 1 )
            {
                IRC.Instance.Send( details.Channel, "{0}: {1}", details.Sender.Nickname, HelpText );
                return;
            }

            int bugId;
            if ( !int.TryParse( details.Args[ 0 ], out bugId ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: Invalid bug ID", details.Sender.Nickname );
                return;
            }

            IRC.Instance.Send( details.Channel, "{0}: https://bugzilla.mozilla.org/show_bug.cgi?id={1}", details.Sender.Nickname, bugId );
        }
    }
}
