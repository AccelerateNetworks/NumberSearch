using Flurl.Http;

using Serilog;

namespace NumberSearch.DataAccess.PortControl
{
    public readonly record struct NumberCNAM(string phoneNumber, string spId, string originalSpId, string providerName, string type, string status, string ocn, string npacRegion, DateTime activationDate, string lata, string lataName, string state, string rateCenter, string lrn, string billingId, string userLocationValue, string userLocationType, Networkdata networkData, Optionaldata optionalData)
    {
        /// <summary>
        /// Query the PortControl API for the CNAM information on a dialed number. https://api.porting.com/swagger/index.html
        /// </summary>
        /// <param name="query">A dialed number.</param>
        /// <param name="accessToken">An access token for the PortControl API.</param>
        /// <returns>The CNAM information on the dialed number.</returns>
        public static async Task<NumberCNAM> GetAsync(ReadOnlyMemory<char> tn, ReadOnlyMemory<char> XPortControlTenant, ReadOnlyMemory<char> accessToken)
        {
            ReadOnlySpan<char> baseUrl = "https://api.porting.com/";
            ReadOnlySpan<char> endpoint = "api/number/";
            ReadOnlySpan<char> endpointSuffix = "cnam";

            string route = $"{baseUrl}{endpoint}{tn}{endpointSuffix}";

            try
            {
                return await route.WithHeader("X-PortControl-Tenant", XPortControlTenant.ToString())
                    .WithOAuthBearerToken(accessToken.ToString()).GetJsonAsync<NumberCNAM>();
            }
            catch (FlurlHttpException ex)
            {
                var x = await ex.GetResponseStringAsync();
                Log.Warning(await ex.GetResponseStringAsync());
                var y = await ex.GetResponseJsonAsync<NumberCNAMError>();
                // Can't return this yet?
                return new();
            }
        }
    }

    // The errors from this endpoint are returned as JSON in the format below.
    public readonly record struct NumberCNAMError(string result, string message);

}
