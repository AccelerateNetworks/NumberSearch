using Flurl.Http;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.InvoiceNinja
{
    public class TaxRate
    {
        public TaxRateDatum[] data { get; set; } = Array.Empty<TaxRateDatum>();

        public static async Task<TaxRate> GetAllAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "tax_rates";
            string tokenHeader = "X-Api-Token";
            string perPageParameter = "?per_page=10000";
            string url = $"{baseUrl}{endpoint}{perPageParameter}";

            return await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<TaxRate>()
                .ConfigureAwait(false);
        }
    }

    public class TaxSingle
    {
        public TaxRateDatum data { get; set; } = new();
    }

    public class TaxRateDatum
    {
        public string account_key { get; set; } = string.Empty;
        public bool is_owner { get; set; }
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public decimal rate { get; set; } = 0M;
        public bool is_inclusive { get; set; }
        public int updated_at { get; set; }
        public object archived_at { get; set; } = new();

        public async Task<TaxRateDatum> PostAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "tax_rates";
            string tokenHeader = "X-Api-Token";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string url = $"{baseUrl}{endpoint}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .WithHeader(contentHeader, contentHeaderValue)
                .PostJsonAsync(new { name, rate })
                .ReceiveJson<TaxSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result?.data ?? new();
        }
    }
}