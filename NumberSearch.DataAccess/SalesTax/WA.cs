using Flurl.Http;

using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class SalesTax
    {
        public static async Task<decimal> GetAsync(string streetAddress, string city, string zip)
        {
            string baseUrl = "https://webgis.dor.wa.gov/webapi/";
            string endpoint = "AddressRates.aspx";
            string outputParameter = $"?output=xml";
            string addrParameter = $"&addr={streetAddress}";
            string cityParameter = $"&city={city}";
            string zipParameter = $"&zip={zip}";
            string url = $"{baseUrl}{endpoint}{outputParameter}{addrParameter}{cityParameter}{zipParameter}";

            var result = await url.GetStringAsync().ConfigureAwait(false);

            var rateKey = " rate=\"";
            var rateValueStart = result.IndexOf(rateKey);
            var rateValueLength = 4;
            var unparsedRate = result.Substring(rateKey.Length + rateValueStart, rateValueLength);
            var checkRate = decimal.TryParse(unparsedRate, out var rate);

            return checkRate ? rate : 0.0M;
        }
    }
}