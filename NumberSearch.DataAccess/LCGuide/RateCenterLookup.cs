using Flurl.Http;

using System.Threading.Tasks;

namespace NumberSearch.DataAccess.LCGuide
{
    public class RateCenterLookup
    {
        public string RateCenter { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;

        public static async Task<RateCenterLookup> GetAsync(string npa, string nxx)
        {
            string baseUrl = "https://localcallingguide.com/xmlprefix.php?";
            string npaParameter = $"npa={npa}";
            string nxxParameter = $"&nxx={nxx}";
            string route = $"{baseUrl}{npaParameter}{nxxParameter}";

            var result = await route.GetStringAsync().ConfigureAwait(false);

            if (result.Contains("</rc>") && result.Contains("<region>"))
            {
                var rateCenterStart = result.IndexOf("<rc>") + "<rc>".Length;
                var rateCenterEnd = result.IndexOf("</rc>");
                var rateCenterText = result[rateCenterStart..rateCenterEnd];

                var regionStart = result.IndexOf("<region>") + "<region>".Length;
                var regionEnd = result.IndexOf("</region>");
                var regionText = result[regionStart..regionEnd];

                return new RateCenterLookup { RateCenter = rateCenterText, Region = regionText };
            }
            else
            {
                return new();
            }
        }
    }
}
