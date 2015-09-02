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

            // add some common triggers for some enums
            Triggers.Add( "!emsg" );
            Triggers.Add( "!eresult" );
        }

        protected override void OnRun( CommandDetails details )
        {
            if ( details.Trigger != "!enum" )
            {
                // if the user didn't use !enum, then it was one of the shortcut commands
                // so we'll rerun the trigger with the enum as the first argument

                string enumName = details.Trigger.Substring( 1 );
                string[] oldArgs = details.Args;

                details.Trigger = "!enum";
                details.Args = new string[ oldArgs.Length + 1 ];

                // new args = enumName + old args
                details.Args[ 0 ] = enumName;
                Array.Copy( oldArgs, 0, details.Args, 1, oldArgs.Length );

                OnRun( details );
                return;
            }

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
                IRC.Instance.Send( details.Channel, "{0}: {1} ({2:D}) = {3}", details.Sender.Nickname, enumName, enumValue, ExpandEnumFlags( enumValue ) );
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

        public static string ExpandEnumFlags<TEnum>( TEnum enumValue )
        {
            Type enumType = typeof( TEnum );
            Type underlyingType = enumType.GetEnumUnderlyingType();

            if ( enumType.GetAttribute<FlagsAttribute>() == null )
            {
                // if this isn't a flag enum, extracting individual bits doesn't make much sense
                return enumValue.ToString();
            }

            ulong flagValue = Convert.ToUInt64( enumValue );
            int bitsRequired = (int)Math.Log( flagValue, 2 ) + 1;

            var flagStrings = new List<string>();

            for ( int bit = 0 ; bit < bitsRequired ; ++bit )
            {
                ulong testFlag = (1UL << bit);

                if ( (flagValue & testFlag) == 0 )
                {
                    // this bit isn't set, skip
                    continue;
                }

                if ( Enum.IsDefined( enumType, Convert.ChangeType( testFlag, underlyingType ) ) )
                {
                    // if the enum defines this value, use the name
                    flagStrings.Add( Enum.GetName( enumType, testFlag ) );
                }
                else
                {
                    // otherwise we can only know what bit was set
                    flagStrings.Add( string.Format( "(1<<{0})", bit ) );
                }
            }

            if ( flagStrings.Count > 0 )
            {
                return string.Join( ", ", flagStrings );
            }

            return enumValue.ToString(); 
        }
    }
}
