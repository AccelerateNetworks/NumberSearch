using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public readonly record struct TaxRate(string name, int locationCode, decimal state, decimal local, decimal rta, decimal rate, DateTime effectiveDate, DateTime expirationDate)
    {
        public static async Task<TaxRate> GetSalesTaxAsync(ReadOnlyMemory<char> streetAddress, ReadOnlyMemory<char> city, ReadOnlyMemory<char> zip)
        {
            string baseUrl = "https://wataxlookup.acceleratenetworks.com/";
            string endpoint = "GetTaxRate";
            string addrParameter = $"?addr={streetAddress}";
            string cityParameter = $"&city={city}";
            string zipParameter = $"&zip={zip}";
            string url = $"{baseUrl}{endpoint}{addrParameter}{cityParameter}{zipParameter}";

            var result = await url.GetJsonAsync<TaxRate>();

            return result;
        }
    };
}