using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using SteamKit2;
using System.Collections;

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

        public static string ToActualString( this IEnumerable<char> value )
        {
            return new string( value.ToArray() );
        }

        // adapted from http://stackoverflow.com/a/13503860/139147
        public static IEnumerable<TResult> FullOuterJoin<TLeft, TRight, TKey, TResult>(
            this IEnumerable<TLeft> left,
            IEnumerable<TRight> right,
            Func<TLeft, TKey> leftKeySelector,
            Func<TRight, TKey> rightKeySelector,
            Func<TLeft, TRight, TKey, TResult> resultSelector,
            TLeft defaultLeft = default( TLeft ),
            TRight defaultRight = default( TRight ) )
        {
            var leftLookup = left.ToLookup( leftKeySelector );
            var rightLookup = right.ToLookup( rightKeySelector );

            var leftKeys = leftLookup.Select( l => l.Key );
            var rightKeys = rightLookup.Select( r => r.Key );

            var keySet = new HashSet<TKey>( leftKeys.Union( rightKeys ) );

            return from key in keySet
                   from leftValue in leftLookup[ key ].DefaultIfEmpty( defaultLeft )
                   from rightValue in rightLookup[ key ].DefaultIfEmpty( defaultRight )
                   select resultSelector( leftValue, rightValue, key );

        }
    }

    static class Utils
    {
        public static DateTime DateTimeFromUnixTime( uint unixTime )
        {
            DateTime origin = new DateTime( 1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc );
            return origin.AddSeconds( unixTime );
        }
        public static uint DateTimeToUnixTime( DateTime time )
        {
            DateTime origin = new DateTime( 1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc );
            return ( uint )( time - origin ).TotalSeconds;
        }

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
            else if ( input.StartsWith( "[", StringComparison.OrdinalIgnoreCase ) )
            {
                if ( steamId.SetFromSteam3String( input ) )
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
                iface.Timeout = ( int )TimeSpan.FromSeconds( 5 ).TotalMilliseconds;

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

            return string.Format( "{0} / {1} (UInt64 = {2}, IsValid = {3}, Universe = {4}, Instance = {5}, Type = {6}, AccountID = {7})",
                input.Render(), input.Render( true ), input.ConvertToUInt64(), input.IsValid, input.AccountUniverse, displayInstance, input.AccountType, input.AccountID );
        }

        public static string ExpandGameID( GameID input )
        {
            return string.Format( "{0} (Type = {1}, AppID = {2}, ModID = {3})",
                input, input.AppType, input.AppID, input.ModID
            );
        }
    }

    class EmptyGrouping<TKey, TValue> : IGrouping<TKey, TValue>
    {
        public TKey Key { get; set; }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            return Enumerable.Empty<TValue>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Enumerable.Empty<TValue>().GetEnumerator();
        }
    }

    class RedirectDownloadStringCompletedEventArgs : EventArgs
    {
        public DownloadStringCompletedEventArgs EventArgs { get; set; }
        public Uri RedirectUri { get; set; }
    }

    class RedirectWebClient : WebClient
    {
        public Uri ResponseUri { get; private set; }

        public event EventHandler<RedirectDownloadStringCompletedEventArgs> RedirectDownloadStringCompleted;


        protected override WebResponse GetWebResponse( WebRequest request, IAsyncResult result )
        {
            var response = base.GetWebResponse( request, result );

            ResponseUri = response.ResponseUri;

            return response;
        }

        protected override void OnDownloadStringCompleted( DownloadStringCompletedEventArgs e )
        {
            var eventArgs = new RedirectDownloadStringCompletedEventArgs();

            eventArgs.EventArgs = e;
            eventArgs.RedirectUri = ResponseUri;

            RedirectDownloadStringCompleted( this, eventArgs );

            base.OnDownloadStringCompleted( e );
        }
    }

}
