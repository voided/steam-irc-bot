using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SteamIrcBot
{
    class PlayerCountHandler : SteamHandler
    {
        public class PlayerCountDetails
        {
            public EResult Result { get; set; }

            public uint AppID { get; set; }
            public uint Count { get; set; }
        }


        public PlayerCountHandler( CallbackManager manager )
            : base( manager )
        {
        }

        public async Task<PlayerCountDetails> RequestPlayerCountWeb( uint appId )
        {
            using ( dynamic api = WebAPI.GetAsyncInterface( "ICommunityService", Settings.Current.WebAPIKey ) )
            {
                KeyValue resultKv;

                try
                {
                    resultKv = await api.GetPlayerCount( appIds: new[] { appId } );
                }
                catch (WebException ex)
                {
                    Log.WriteError( nameof( PlayerCountHandler ), "Unable to retreive player count from WebAPI: {0}", ex );

                    return new PlayerCountDetails
                    {
                        AppID = appId,
                        Result = EResult.Fail,
                    };
                }

                KeyValue playerCount = resultKv["apps_played"].Children
                    .FirstOrDefault( c => c["appid"].AsInteger() == appId );

                if ( playerCount == null )
                {
                    return new PlayerCountDetails
                    {
                        AppID = appId,
                        Result = EResult.NoMatch,
                    };
                }

                return new PlayerCountDetails
                {
                    AppID = appId,
                    Result = EResult.OK,

                    Count = playerCount["players"].AsUnsignedInteger(),
                };
            }
        }

        public async Task<PlayerCountDetails> RequestPlayerCount( uint appId )
        {
            var req = new CCommunity_GetPlayerCount_Request
            {
                appids = { appId },
            };

            var callback = await Steam.Instance.Community.SendMessage( api => api.GetPlayerCount( req ) );

            if ( callback.Result != EResult.OK )
            {
                return new PlayerCountDetails
                {
                    AppID = appId,
                    Result = callback.Result
                };
            }

            var response = callback.GetDeserializedResponse<CCommunity_GetPlayerCount_Response>();

            var played = response.apps_played
                .FirstOrDefault( app => app.appid == appId );

            if ( played == null )
            {
                return new PlayerCountDetails
                {
                    AppID = appId,
                    Result = EResult.NoMatch,
                };
            }

            return new PlayerCountDetails
            {
                AppID = appId,
                Result = callback.Result,

                Count = played.players,
            };
        }

        public override void Tick()
        {
            // nop
        }
    }
}
