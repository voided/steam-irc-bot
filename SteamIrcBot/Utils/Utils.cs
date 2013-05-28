using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using SteamKit2;

namespace SteamIrcBot
{
    static class ExtensionUtils
    {
        public static T GetAttribute<T>( this Type type, bool inherit )
            where T : Attribute
        {
            T[] attribs = type.GetCustomAttributes( typeof( T ), inherit ) as T[];

            if ( attribs == null || attribs.Length == 0 )
                return null;

            return attribs[ 0 ];
        }

        public static bool Implements( this Type type, Type interfaceType )
        {
            return type.GetInterfaces()
                .Any( i => i == interfaceType );
        }

        public static string Truncate( this string value, int length )
        {
            if ( string.IsNullOrEmpty( value ) )
                return value;

            if ( value.Length <= length )
                return value;

            return value.Substring( 0, length ) + "...";
        }

        public static string Clean( this string value )
        {
            if ( string.IsNullOrEmpty( value ) )
                    return value;

            value = Regex.Replace( value, @"\s+", " " ); // remove excess whitespace
            value = Regex.Replace( value, @"\p{C}+", "" ); // remove control codes

            return value;
        }
    }

    static class Utils
    {
        public static string GetByteSizeString( uint size )
        {
            string[] suf = { "B", "KB", "MB", "GB" };

            if ( size == 0 )
                return "0B";

            int place = Convert.ToInt32( Math.Floor( Math.Log( size, 1024 ) ) );
            double num = Math.Round( size / Math.Pow( 1024, place ), 1 );
            return ( Math.Sign( size ) * num ).ToString() + suf[ place ];
        }
    }

    static class SteamUtils
    {
        public static bool TryDeduceSteamID( string input, out SteamID steamId )
        {
            steamId = new SteamID();

            if ( string.IsNullOrEmpty( input ) )
                return false;

            if ( input.StartsWith( "STEAM_", StringComparison.OrdinalIgnoreCase ) )
            {
                steamId = new SteamID( input, EUniverse.Public );
                return true;
            }

            ulong uSteamID;
            if ( ulong.TryParse( input, out uSteamID ) )
            {
                steamId = uSteamID;
                return true;
            }

            if ( ResolveVanityURL( input, out steamId ) )
                return true;

            return false;
        }

        public static bool ResolveVanityURL( string customUrl, out SteamID steamId )
        {
            steamId = new SteamID();

            if ( string.IsNullOrWhiteSpace( customUrl ) )
                return false;

            var apiKey = Settings.Current.WebAPIKey;

            if ( apiKey == null )
            {
                Log.WriteWarn( "SteamUtils", "Unable to use ResolveVanityURL: no web api key in settings" );
                return false;
            }

            using ( dynamic iface = WebAPI.GetInterface( "ISteamUser", apiKey ) )
            {
                KeyValue results = null;

                try
                {
                    results = iface.ResolveVanityURL( vanityurl: customUrl );
                }
                catch ( WebException )
                {
                    return false;
                }

                EResult eResult = ( EResult )results[ "success" ].AsInteger();

                if ( eResult == EResult.OK )
                {
                    steamId = ( ulong )results[ "steamid" ].AsLong();
                    return true;
                }
            }

            return false;
        }

        public static string ExpandGID( GlobalID input )
        {
            return string.Format( "{0} (SeqCount = {1}, StartTime = {2}, ProcessID = {3}, BoxID = {4})",
                ( ulong )input, input.SequentialCount, input.StartTime, input.ProcessID, input.BoxID );
        }

        public static string ExpandSteamID( SteamID input )
        {
            string displayInstance = input.AccountInstance.ToString();

            switch ( input.AccountInstance )
            {
                case SteamID.AllInstances:
                    displayInstance = "all (0)";
                    break;

                case SteamID.DesktopInstance:
                    displayInstance = "desktop (1)";
                    break;

                case SteamID.ConsoleInstance:
                    displayInstance = "console (2)";
                    break;

                case SteamID.WebInstance:
                    displayInstance = "web (4)";
                    break;

                case ( uint )SteamID.ChatInstanceFlags.Clan:
                    displayInstance = "clan (524288 / 0x80000)";
                    break;

                case ( uint )SteamID.ChatInstanceFlags.Lobby:
                    displayInstance = "lobby (262144 / 0x40000)";
                    break;

                case ( uint )SteamID.ChatInstanceFlags.MMSLobby:
                    displayInstance = "mms lobby (131072 / 0x20000)";
                    break;
            }

            return string.Format( "{0} (UInt64 = {1}, IsValid = {2}, Universe = {3}, Instance = {4}, Type = {5}, AccountID = {6})",
                input.ToString(), input.ConvertToUInt64(), input.IsValid, input.AccountUniverse, displayInstance, input.AccountType, input.AccountID );
        }
    }

}
