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
        const string TICKER = "http://data.bter.com/api/1/ticker/doge_btc";


        public class Request : BaseRequest
        {
        }


        public DOGECommand()
        {
            Triggers.Add( "!doge" );
            Triggers.Add( "!tothemoon" );

            HelpText = "!doge - Request current DOGE prices in BTC";
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

            string bid, ask;

            try
            {
                dynamic tickerData = JObject.Parse( e.Result );

                bid = tickerData.buy;
                ask = tickerData.sell;
            }
            catch ( Exception ex )
            {
                IRC.Instance.Send( req.Channel, "{0}: An error occurred while parsing the response from the BTER API", req.Requester.Nickname );
                Log.WriteWarn( "DOGECommand", "Parse error: {0}", ex );
                return;
            }

            IRC.Instance.Send( req.Channel, "{0}: Bid: ${1} USD - Ask: ${2} USD", req.Requester.Nickname, bid, ask );
        }
    }
}
