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
                IRC.Instance.Send( details.Channel, "{0}: enum and value or filter arguments required", details.Sender.Nickname );
                return;
            }

            string enumType = details.Args[ 0 ];
            string inputEnum = details.Args[ 1 ];

            var matchingEnumType = typeof( CMClient ).Assembly.GetTypes()
                .Where( x => x.IsEnum )
                // we want to match against name matches, or partial fullname matches
                .Where( x => x.Namespace != null && x.Namespace.StartsWith( "SteamKit2" ) )
                // some inner namespaces have enums that have matching names, but we (most likely) want to match against the root enums
                // so we order by having the root enums first
                .OrderByDescending( x => x.Namespace == "SteamKit2" )
                .FirstOrDefault( x => x.Name.Equals( enumType, StringComparison.InvariantCultureIgnoreCase ) || x.GetDottedTypeName().IndexOf( enumType, StringComparison.OrdinalIgnoreCase ) != -1);

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
            string enumName = typeof( TEnum ).GetDottedTypeName();

            TEnum enumValue;
            if ( Enum.TryParse( inputValue, out enumValue ) )
            {
                var enumFieldName = Enum.GetName( typeof ( TEnum ), enumValue );
                if (string.IsNullOrEmpty( enumFieldName ))
                    enumFieldName = "<unknown>";

                var resultFormatted = enumFieldName.Equals( inputValue, StringComparison.InvariantCultureIgnoreCase )
                    ? string.Format( "{0:D}", enumValue as Enum )
                    : enumFieldName;

                IRC.Instance.Send( details.Channel, "{0}: {1} ({2}) = {3}", details.Sender.Nickname, enumName, inputValue, resultFormatted );
            }
            else
            {
                bool includeDeprecated = details.Args.Length > 2
                    && details.Args[ 2 ].Equals( "deprecated", StringComparison.InvariantCultureIgnoreCase );

                var enumValues = Enum.GetValues( typeof( TEnum ) ).Cast<TEnum>();
                if ( !includeDeprecated )
                {
                    enumValues = enumValues.Except( enumValues.Where( x => typeof( TEnum ).GetMember( x.ToString() )[ 0 ].GetCustomAttributes( typeof( ObsoleteAttribute ), inherit: false ).Any() ) );
                }

                var enumValuesWithMatchingName = enumValues.Where( x => x.ToString().IndexOf( inputValue, StringComparison.InvariantCultureIgnoreCase ) >= 0 );
                if ( !enumValuesWithMatchingName.Any() )
                {
                    IRC.Instance.Send( details.Channel, "{0}: No matches found.", details.Sender.Nickname );
                }
                else if ( enumValuesWithMatchingName.Count() > 10 )
                {
                    IRC.Instance.Send( details.Channel, "{0}: More than 10 results found.", details.Sender.Nickname );
                }
                else
                {
                    var formatted = string.Join( ", ", enumValuesWithMatchingName.Select( @enum => string.Format( "{0} ({1})", @enum.ToString(), Enum.Format( typeof( TEnum ), @enum, "D" ) ) ) );
                    IRC.Instance.Send( details.Channel, "{0}: {1} = {2}", details.Sender.Nickname, enumName, formatted );
                }
            }
        }
    }
}
