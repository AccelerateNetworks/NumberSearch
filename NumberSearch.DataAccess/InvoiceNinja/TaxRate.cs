using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.InvoiceNinja
{
    public readonly record struct TaxRate(TaxRateDatum[] data)
    {
        public static async Task<TaxRate> GetAllAsync(ReadOnlyMemory<char> token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "tax_rates";
            string tokenHeader = "X-Api-Token";
            string perPageParameter = "?per_page=10000";
            string url = $"{baseUrl}{endpoint}{perPageParameter}";

            return await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<TaxRate>();
        }
    }

    public readonly record struct TaxSingle(TaxRateDatum data);

    public readonly record struct TaxRateDatum
    (
        string account_key,
        bool is_owner,
        string id,
        string name,
        decimal rate,
        bool is_inclusive,
        int updated_at,
        object archived_at
    ) {
        public async Task<TaxRateDatum> PostAsync(ReadOnlyMemory<char> token)
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
                .ReceiveJson<TaxSingle>();

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }
    }
}