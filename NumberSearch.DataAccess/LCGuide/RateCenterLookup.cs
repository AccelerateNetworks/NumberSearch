using Flurl.Http;

using System.Threading.Tasks;

namespace NumberSearch.DataAccess.LCGuide
{
    public class RateCenterLookup
    {
        public string RateCenter { get; set; }
        public string Region { get; set; }

        public static async Task<RateCenterLookup> GetAsync(string npa, string nxx)
        {
            string baseUrl = "https://localcallingguide.com/xmlprefix.php?";
            string npaParameter = $"npa={npa}";
            string nxxParameter = $"&nxx={nxx}";
            string route = $"{baseUrl}{npaParameter}{nxxParameter}";

            var result = await route.GetStringAsync().ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(result))
            {
                var rateCenterStart = result.IndexOf("<rc>") + "<rc>".Length;
                var rateCenterEnd = result.IndexOf("</rc>");
                var rateCenterText = result.Substring(rateCenterStart, rateCenterEnd - rateCenterStart);

                var regionStart = result.IndexOf("<region>") + "<region>".Length;
                var regionEnd = result.IndexOf("</region>");
                var regionText = result.Substring(regionStart, regionEnd - regionStart);

                return new RateCenterLookup { RateCenter = rateCenterText, Region = regionText };
            }
            else
            {
                return null;
            }
        }
    }
}
