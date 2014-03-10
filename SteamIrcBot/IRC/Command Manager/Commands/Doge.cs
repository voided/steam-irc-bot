using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SteamIrcBot
{
    class DOGECommand : Command<DOGECommand.Request>
    {
        const string TICKER = "https://dogeapi.com/wow/v2/?a=get_info";

        public class Request : BaseRequest
        {
        }


        public DOGECommand()
        {
            Triggers.Add( "!doge" );
            Triggers.Add( "!tothemoon" );

            HelpText = "!doge - Request current DOGE prices in USD & BTC";
        }

        protected override void OnRun( CommandDetails details )
        {
            using ( var webClient = new WebClient() )
            {
                var req = new Request();
                AddRequest( details, req );

                webClient.DownloadStringCompleted += OnDownloadStringCompleted;
                webClient.DownloadStringAsync( new Uri( TICKER ), req );
            }
        }

        void OnDownloadStringCompleted( object sender, DownloadStringCompletedEventArgs e )
        {
            var req = GetRequest( r => e.UserState == r );

            if ( req == null )
                return;

            if ( e.Error != null )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request current DOGE prices: {1}", req.Requester.Nickname, e.Error.Message );
                return;
            }

            string usd, btc, difficulty, networkhashrate, currentblock;

            try
            {
                dynamic tickerData = JObject.Parse( e.Result );

                usd = tickerData.data.info.doge_usd;
                btc = tickerData.data.info.doge_btc;
                difficulty = tickerData.data.info.difficulty;
                networkhashrate = tickerData.data.info.network_hashrate;
                currentblock = tickerData.data.info.current_block;
            }
            catch ( Exception ex )
            {
                IRC.Instance.Send( req.Channel, "{0}: An error occurred while parsing the response from the DOGE API", req.Requester.Nickname );
                Log.WriteWarn( "DOGECommand", "Parse error: {0}", ex );
                return;
            }

            IRC.Instance.Send( req.Channel, "{0}: ${1}USD / ${2}BTC (Difficulty: ${3} Network Hashrate: ${4} Block: ${5})", req.Requester.Nickname, usd, btc, difficulty, networkhashrate, currentblock );
        }
    }
}
