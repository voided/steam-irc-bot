using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamIrcBot
{
    class TopCustomsCommand : Command<TopCustomsCommand.Request>
    {
        public class Request : BaseRequest
        {
        }

        public TopCustomsCommand()
        {
            Triggers.Add( "!topcustoms" );

            HelpText = "!topcustoms - Show the top custom dota games";
        }

        protected override void OnRun( CommandDetails details )
        {
            string displayString = DotaGCTopGamesHandler.Instance.GetDisplay();

            IRC.Instance.Send( details.Channel, "{0}: {1}", details.Sender.Nickname, displayString );
        }
    }
}
