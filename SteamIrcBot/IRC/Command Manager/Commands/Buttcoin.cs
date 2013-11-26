using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SteamIrcBot
{
    class ButtcoinCommand : Command<ButtcoinCommand.Request>
    {
        const string MTGOX_TICKER = "http://data.mtgox.com/api/2/BTCUSD/money/ticker_fast";


        public class Request : BaseRequest
        {
        }


        public ButtcoinCommand()
        {
            Triggers.Add( "!btc" );
            Triggers.Add( "!bitcoin" );
            Triggers.Add( "!buttcoin" );

            HelpText = "!btc - Request current MtGox BTC prices";
        }

        protected override void OnRun( CommandDetails details )
        {
            using ( var webClient = new WebClient() )
            {
                var req = new Request();
                AddRequest( details, req );

                webClient.DownloadStringCompleted += OnDownloadStringCompleted;
                webClient.DownloadStringAsync( new Uri( MTGOX_TICKER ), req );
            }
        }

        void OnDownloadStringCompleted( object sender, DownloadStringCompletedEventArgs e )
        {
            var req = GetRequest( r => e.UserState == r );

            if ( req == null )
                return;

            if ( e.Error != null )
            {
                IRC.Instance.Send( req.Channel, "{0}: Unable to request current BTC prices: {1}", req.Requester.Nickname, e.Error.Message );
                return;
            }

            bool success = false;
            string bid, ask;

            try
            {
                dynamic tickerData = JObject.Parse( e.Result );

                success = tickerData.result == "success";

                bid = tickerData.data.buy.display;
                ask = tickerData.data.sell.display;
            }
            catch ( Exception ex )
            {
                IRC.Instance.Send( req.Channel, "{0}: An error occurred while parsing the response from the MtGox API", req.Requester.Nickname );
                Log.WriteWarn( "ButtcoinCommand", "Parse error: {0}", ex );
                return;
            }

            if ( !success )
            {
                IRC.Instance.Send( req.Channel, "{0}: The MtGox API encountered an error", req.Requester.Nickname );
                return;
            }

            IRC.Instance.Send( req.Channel, "{0}: Bid: {1} USD - Ask: {2} USD", req.Requester.Nickname, bid, ask );
        }
    }
}
