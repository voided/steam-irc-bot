using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamIrcBot
{
    class TI4Command : Command
    {
        DateTime WildCard = new DateTime( 2014, 5 /*may*/, 12, 12 + 8 /*pm*/, 0, 0, DateTimeKind.Utc );
        DateTime GroupStages = new DateTime( 2014, 7 /*july*/, 8, 12 + 4 /*pm*/, 0, 0, DateTimeKind.Utc );
        DateTime MainEvent = new DateTime( 2014, 7 /*july*/, 18, 12 + 7 /*pm*/, 0, 0, DateTimeKind.Utc );


        public TI4Command()
        {
            Triggers.Add( "!ti4" );
            HelpText = "!ti4 - Countdown to Doters";
        }


        protected override void OnRun( CommandDetails details )
        {
            TimeSpan timeToWildCard = WildCard - DateTime.UtcNow;
            TimeSpan timeToGroupStages = GroupStages - DateTime.UtcNow;
            TimeSpan timeToManEvent = MainEvent - DateTime.UtcNow;

            IRC.Instance.Send( details.Channel, "{0}: Wild Card: {1} | Group Stages: {2} | Main Event: {3} | ~Times TBA~ Sauce: http://goo.gl/wcwQMf",
                details.Sender.Nickname, GetTime( timeToWildCard ), GetTime( timeToGroupStages ), GetTime( timeToManEvent ) );
        }

        string GetTime( TimeSpan input )
        {
            bool inThePast = input < TimeSpan.Zero;

            if ( inThePast )
                input = input.Negate();

            return string.Format( new PluralizeFormatProvider(), "{0:day/days}, {1:hour/hours}, {2:minute/minutes}, {3:second/seconds} {4}", input.Days, input.Hours, input.Minutes, input.Seconds, inThePast ? "ago" : "" );
        }


        class PluralizeFormatProvider : IFormatProvider, ICustomFormatter
        {
            /// <summary>
            /// Returns an object that provides formatting services for the specified type.
            /// </summary>
            /// <param name="formatType">An object that specifies the type of format object to return.</param>
            /// <returns>
            /// An instance of the object specified by <paramref name="formatType" />, if the <see cref="T:System.IFormatProvider" /> implementation can supply that type of object; otherwise, null.
            /// </returns>
            public object GetFormat( Type formatType )
            {
                return this;
            }

            /// <summary>
            /// Converts the value of a specified object to an equivalent string representation using specified format and culture-specific formatting information.
            /// </summary>
            /// <param name="format">A format string containing formatting specifications.</param>
            /// <param name="arg">An object to format.</param>
            /// <param name="formatProvider">An object that supplies format information about the current instance.</param>
            /// <returns>
            /// The string representation of the value of <paramref name="arg" />, formatted as specified by <paramref name="format" /> and <paramref name="formatProvider" />.
            /// </returns>
            public string Format( string format, object arg, IFormatProvider formatProvider )
            {
                if ( format == null )
                    return arg.ToString();

                string[] forms = format.Split( new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries );

                if ( arg is int )
                {
                    int value = ( int )arg;

                    if ( value == 1 )
                        return string.Format( "{0} {1}", value, forms[ 0 ] );

                    return string.Format( "{0} {1}", value, forms[ 1 ] );
                }

                return arg.ToString();
            }
        }
    }
}
