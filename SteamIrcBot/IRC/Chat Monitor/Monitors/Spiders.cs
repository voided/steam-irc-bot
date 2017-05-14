using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SteamIrcBot
{
    class Spiders : BaseMonitor
    {
        private readonly Regex urlRegex = new Regex( @"https?:\/\/[\w_-]+(?:(?:\.[\w_-]+)+)[\w.,@?^=%&:\/~+#-]*[\w@?^=%&\/~+#-]?", RegexOptions.IgnoreCase | RegexOptions.Compiled );
        

        protected async override void OnMessage( MessageDetails msgDetails )
        {
            if ( !Settings.Current.DoesChannelHaveTag( msgDetails.Channel, "spiders" ) )
            {
                // channel isn't configured for spider detection
                return;
            }

            if ( string.IsNullOrEmpty( Settings.Current.CognitiveVisionKey ) )
            {
                // api key is missing, not much to do
                return;
            }

            var urls = GetUrls( msgDetails.Message );

            var client = new VisionServiceClient( Settings.Current.CognitiveVisionKey, Settings.Current.CognitiveVisionEndpoint );

            foreach ( var url in urls )
            {
                AnalysisResult result = null;

                try
                {
                    result = await client.AnalyzeImageAsync(url, new[] { VisualFeature.Description, VisualFeature.Tags });
                }
                catch ( Exception ex )
                {
                    Log.WriteError( "Spiders", "Error occurred while submitting imae for analysis: {0}", ex );
                    return;
                }

                var spiderResult = result?.Tags?
                    .FirstOrDefault(t =>
                    {
                        if ( string.Equals( t.Name, "spider" ) )
                            return t.Confidence > 0.40;

                        if (string.Equals(t.Name, "arthropod"))
                            return t.Confidence > 0.80;

                        return false;
                    });
                
                if ( spiderResult != null )
                {
                    // WTF

                    if ( !string.Equals( spiderResult.Name, "spider" ) )
                    {
                        // if we didn't directly match a spider, reduce our confidence level
                        spiderResult.Confidence -= 0.40;
                    }

                    double confidencePercent = spiderResult.Confidence * 100.0;
                    IRC.Instance.Send( msgDetails.Channel, $"🕷 Alert! I'm like {confidencePercent:0.00}% sure there's a spider in {url}" );

                    return;
                }
            }
        }

        private IEnumerable<string> GetUrls(string message)
        {
            return urlRegex.Matches(message)
                .OfType<Match>()
                .Select(m =>
                {
                    return m.Value;
                });
        }
    }
}
