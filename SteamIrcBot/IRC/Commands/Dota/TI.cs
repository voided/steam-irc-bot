using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamIrcBot
{
    class TI6Command : TICommand
    {
        DateTime MainEvent = new DateTime( 2016, 8 /*august*/, 8, 0, 0, 0, DateTimeKind.Utc );

        public TI6Command()
        {
            Triggers.Add( "!ti6" );
            HelpText = "!ti6 - Countdown to Doters";
        }

        protected override void OnRun( CommandDetails details )
        {
            TimeSpan timeToMainEvent = MainEvent - DateTime.UtcNow;

            IRC.Instance.Send( details.Channel, "{0}: TI6 Main Event: {1}",
                details.Sender.Nickname, GetTime( timeToMainEvent )
            );
        }
    }

    class TI5Command : TICommand
    {
        // don't have any more information about the date yet
        DateTime Qualifiers = new DateTime( 2015, 5 /*may*/, 25, 0, 0, 0, DateTimeKind.Utc );
        DateTime MainEvent = new DateTime( 2015, 8 /*august*/, 3, 0, 0, 0, DateTimeKind.Utc );

        public TI5Command()
        {
            Triggers.Add( "!ti5" );
            HelpText = "!ti5 - Countdown to Doters";
        }

        protected override void OnRun( CommandDetails details )
        {
            TimeSpan timeToQualifiers = Qualifiers - DateTime.UtcNow;
            TimeSpan timeToMainEvent = MainEvent - DateTime.UtcNow;

            IRC.Instance.Send( details.Channel, "{0}: TI5 Qualifiers: {1} | Main Event: {2}",
                details.Sender.Nickname, GetTime( timeToQualifiers ), GetTime( timeToMainEvent )
            );
        }
    }

    class TI4Command : TICommand
    {
        DateTime RegionalQualifiers = new DateTime( 2014, 5 /*april*/, 12, 0, 0, 0, DateTimeKind.Utc );
        DateTime Playoffs = new DateTime( 2014, 7 /*july*/, 8, 0, 0, 0, DateTimeKind.Utc );
        DateTime MainEvent = new DateTime( 2014, 7 /*july*/, 18, 0, 0, 0, DateTimeKind.Utc );

        public TI4Command()
        {
            Triggers.Add( "!ti4" );
            HelpText = "!ti4 - Countdown to Doters";
        }

        protected override void OnRun( CommandDetails details )
        {
            TimeSpan timeToRegionalQuals = RegionalQualifiers - DateTime.UtcNow;
            TimeSpan timeToPlayoffs = Playoffs - DateTime.UtcNow;
            TimeSpan timeToMainEvent = MainEvent - DateTime.UtcNow;

            IRC.Instance.Send( details.Channel, "{0}: TI4 Regional Qualifiers: {1} | Playoffs: {2} | Main Event: {3}",
                details.Sender.Nickname, GetTime( timeToRegionalQuals ), GetTime( timeToPlayoffs ), GetTime( timeToMainEvent ) );
        }
    }

    class TI3Command : TICommand
    {
        DateTime WildCard = new DateTime( 2013, 8 /*august*/, 2, 12 + 8 /*pm*/, 0, 0, DateTimeKind.Utc );
        DateTime GroupStages = new DateTime( 2013, 8 /*august*/, 3, 12 + 4 /*pm*/, 0, 0, DateTimeKind.Utc );
        DateTime MainEvent = new DateTime( 2013, 8 /*august*/, 7, 12 + 7 /*pm*/, 0, 0, DateTimeKind.Utc );


        public TI3Command()
        {
            Triggers.Add( "!ti3" );
            HelpText = "!ti3 - Countdown to Doters";
        }


        protected override void OnRun( CommandDetails details )
        {
            TimeSpan timeToWildCard = WildCard - DateTime.UtcNow;
            TimeSpan timeToGroupStages = GroupStages - DateTime.UtcNow;
            TimeSpan timeToMainEvent = MainEvent - DateTime.UtcNow;

            IRC.Instance.Send( details.Channel, "{0}: TI3 Wild Card: {1} | Group Stages: {2} | Main Event: {3}",
                details.Sender.Nickname, GetTime( timeToWildCard ), GetTime( timeToGroupStages ), GetTime( timeToMainEvent ) );
        }

    }

    abstract class TICommand : Command
    {
        protected string GetTime( TimeSpan input )
        {
            bool inThePast = input < TimeSpan.Zero;

            if ( inThePast )
                input = input.Negate();

            return string.Format( new PluralizeFormatProvider(), "{0:day/days}, {1:hour/hours}, {2:minute/minutes}, {3:second/seconds} {4}", input.Days, input.Hours, input.Minutes, input.Seconds, inThePast ? "ago" : "" );
        }
    }
}
