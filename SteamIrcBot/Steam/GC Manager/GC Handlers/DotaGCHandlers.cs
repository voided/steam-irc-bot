using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;

namespace SteamIrcBot
{
    class DotaGCTopGamesHandler : GCHandler
    {
        public static DotaGCTopGamesHandler Instance { get; private set; }


        CMsgGCTopCustomGamesList cachedGames;

        UGCHandler ugcHandler;


        public DotaGCTopGamesHandler( GCManager manager )
            : base ( manager )
        {
            Instance = this;

            new GCCallback<CMsgGCTopCustomGamesList>( (uint)EDOTAGCMsg.k_EMsgGCTopCustomGamesList, OnTopCustomGames, manager );

            ugcHandler = Steam.Instance.SteamManager.GetHandler<UGCHandler>();
        }


        void OnTopCustomGames( ClientGCMsgProtobuf<CMsgGCTopCustomGamesList> msg, uint gcAppId )
        {
            cachedGames = msg.Body;

            string displayMsg = GetDisplay();

            IRC.Instance.SendToTag( "gc-dota-verbose", "{0} {1}", Steam.Instance.GetAppName( gcAppId ), displayMsg );
        }


        public string GetDisplay()
        {
            return $"{GetGotDDisplay()} | {GetCustomsDisplay()}";
        }

        public string GetCustomsDisplay()
        {
            if ( cachedGames == null )
            {
                // nothing cached yet
                return "Top customs: unknown";
            }

            int numGames = cachedGames.top_custom_games.Count;

            if ( numGames == 0 )
            {
                // nothing useful to display
                return "Top customs: none";
            }

            var games = cachedGames.top_custom_games.Take( 20 );

            // convert our pubfile ids into user friendly names
            var gameInfos = games.Select( pubFile =>
            {
                string name;

                if ( !ugcHandler.LookupUGCName( pubFile, out name ) )
                {
                    // couldn't look up a name, resort to displaying the pub file
                    return pubFile.ToString();
                }

                return name;
            } );

            return string.Format( "Top customs: {0}", string.Join( ", ", gameInfos ) );
        }

        public string GetGotDDisplay()
        {
            if ( cachedGames == null )
            {
                return "Game of the Day: unknown";
            }

            if ( cachedGames.game_of_the_day == 0 )
            {
                return "Game of the Day: none";
            }

            string gameName;

            if ( !ugcHandler.LookupUGCName( cachedGames.game_of_the_day, out gameName ) )
            {
                gameName = cachedGames.game_of_the_day.ToString();
            }

            return $"Game of the Day: {gameName}";
        }
    }
}
