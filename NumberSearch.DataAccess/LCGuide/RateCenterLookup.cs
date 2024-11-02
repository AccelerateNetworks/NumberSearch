using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.LCGuide
{
    public readonly record struct RateCenterLookup(ReadOnlyMemory<char> RateCenter, ReadOnlyMemory<char> Region)
    {
        public static async Task<RateCenterLookup> GetAsync(ReadOnlyMemory<char> npa, ReadOnlyMemory<char> nxx)
        {
            string baseUrl = "https://localcallingguide.com/xmlprefix.php?";
            string npaParameter = $"npa={npa}";
            string nxxParameter = $"&nxx={nxx}";
            string route = $"{baseUrl}{npaParameter}{nxxParameter}";

            var result = await route.GetStringAsync();

            if (result.Contains("</rc>") && result.Contains("<region>"))
            {
                var rateCenterStart = result.IndexOf("<rc>") + "<rc>".Length;
                var rateCenterEnd = result.IndexOf("</rc>");
                ReadOnlyMemory<char> rateCenterText = result.AsMemory()[rateCenterStart..rateCenterEnd];

                var regionStart = result.IndexOf("<region>") + "<region>".Length;
                var regionEnd = result.IndexOf("</region>");
                ReadOnlyMemory<char> regionText = result.AsMemory()[regionStart..regionEnd];

                return new RateCenterLookup(rateCenterText, regionText);
            }
            else
            {
                return new();
            }
        }
    }
}
