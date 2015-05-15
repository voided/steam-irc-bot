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
            HelpText = "!enum <enumname> [value or substring] [deprecated] - Returns the enum string for a given value, or enum matches for a substring";
        }

        protected override void OnRun( CommandDetails details )
        {
            if ( details.Args.Length < 1 )
            {
                IRC.Instance.Send( details.Channel, "{0}: enum name argument required", details.Sender.Nickname );
                return;
            }

            string enumType = details.Args[ 0 ];
            string inputEnum = details.Args.Length > 1 ? details.Args[ 1 ] : null;

            var matchingEnumType = typeof( CMClient ).Assembly.GetTypes()
                .Where( x => x.IsEnum )
                .Where( x => x.Namespace.StartsWith( "SteamKit2" ) )
                // some inner namespaces have enums that have matching names, but we (most likely) want to match against the root enums
                // so we order by having the root enums first
                .OrderByDescending( x => x.Namespace == "SteamKit2" )
                // we want to match against name matches, or partial fullname matches
                .Where( x => x.Name.Equals( enumType, StringComparison.InvariantCultureIgnoreCase ) || x.GetDottedTypeName().IndexOf( enumType, StringComparison.OrdinalIgnoreCase ) != -1 )
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
            string enumName = typeof( TEnum )
                .GetDottedTypeName()
                .Replace( "SteamKit2.", "" ); // chop off the root namespace

            TEnum enumValue;
            if ( Enum.TryParse<TEnum>( inputValue, out enumValue ) )
            {
                IRC.Instance.Send( details.Channel, "{0}: {1} ({2:D}) = {3}", details.Sender.Nickname, enumName, enumValue, enumValue );
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

                if ( !string.IsNullOrEmpty( inputValue ) )
                {
                    enumValues = enumValues.Where( x => x.ToString().IndexOf( inputValue, StringComparison.InvariantCultureIgnoreCase ) >= 0 );
                }

                int matchingCount = enumValues.Count();
                bool truncated = false;

                if ( matchingCount == 0 )
                {
                    IRC.Instance.Send( details.Channel, "{0}: No matches found.", details.Sender.Nickname );
                    return;
                }
                else if ( matchingCount > 10 )
                {
                    truncated = true;
                    enumValues = enumValues.Take( 10 );
                }

                var formatted = string.Join( ", ", enumValues.Select( @enum => string.Format( "{0} ({1})", @enum.ToString(), Enum.Format( typeof( TEnum ), @enum, "D" ) ) ) );

                if ( truncated )
                {
                    formatted = string.Format( "{0}, and {1} more...", formatted, matchingCount - 10 );
                }

                IRC.Instance.Send( details.Channel, "{0}: {1} = {2}", details.Sender.Nickname, enumName, formatted );
            }
        }
    }
}
