using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SteamKit2;
using SteamKit2.Internal;

namespace SteamIrcBot
{
    class EnumCommand : Command
    {
        public EnumCommand()
        {
            Triggers.Add( "!enum" );
            HelpText = "!enum <enumname> <value or substring> [deprecated] - Returns the enum string for a given value, or enum matches for a substring";
        }

        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length < 2 )
            {
                IRC.Instance.Send( details.Channel, "{0}: EMsg argument required", details.Sender.Nickname );
                return;
            }

            string enumType = details.Args[ 0 ];
            string inputEnum = details.Args[ 1 ];

            var matchingEnumType = typeof( CMClient ).Assembly.GetTypes()
                .Where( x => x.IsEnum )
                .Where( x => x.Namespace == "SteamKit2" )
                .Where( x => x.Name.Equals( enumType, StringComparison.InvariantCultureIgnoreCase ) )
                .FirstOrDefault();

            if ( matchingEnumType == null )
            {
                IRC.Instance.Send( details.Channel, "{0}: No such enum type.", details.Sender.Nickname );
                return;
            }

            GetType().GetMethod( "RunForEnum", BindingFlags.Instance | BindingFlags.NonPublic )
                .MakeGenericMethod( matchingEnumType )
                .Invoke( this, new object[] { inputEnum, details } );
        }

        void RunForEnum<TEnum>(string inputValue, CommandDetails details)
            where TEnum : struct
        {
            TEnum enumValue;
            if ( Enum.TryParse<TEnum>( inputValue, out enumValue ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: {1}", details.Sender.Nickname, enumValue );
            }
            else
            {
                bool includeDeprecated = false;
                if ( details.Args.Length > 2 && details.Args[ 2 ].Equals( "deprecated", StringComparison.InvariantCultureIgnoreCase ) )
                {
                    includeDeprecated = true;
                }

                var enumValues = Enum.GetValues( typeof( TEnum ) ).Cast<TEnum>();
                if ( !includeDeprecated )
                {
                    enumValues = enumValues.Except( enumValues.Where( x => typeof( TEnum ).GetMember( x.ToString() )[ 0 ].GetCustomAttributes( typeof( ObsoleteAttribute ), inherit: false ).Any() ) );
                }

                var enumValuesWithMatchingName = enumValues.Where( x => x.ToString().IndexOf( inputValue, StringComparison.InvariantCultureIgnoreCase ) >= 0 );
                if ( enumValuesWithMatchingName.Count() == 0 )
                {
                    IRC.Instance.Send( details.Channel, "{0}: No matches found.", details.Sender.Nickname );
                }
                else if ( enumValuesWithMatchingName.Count() > 10 )
                {
                    IRC.Instance.Send( details.Channel, "{0}: More than 10 results found.", details.Sender.Nickname );
                }
                else
                {
                    var formatted = string.Join( ", ", enumValuesWithMatchingName.Select( @enum => string.Format( "{0} ({1})", @enum.ToString(), ( int )( object )@enum ) ) );
                    IRC.Instance.Send( details.Channel, "{0}: {1}", details.Sender.Nickname, formatted );
                }
            }
        }
    }
}
