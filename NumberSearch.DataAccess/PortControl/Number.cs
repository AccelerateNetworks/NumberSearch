using Flurl.Http;

using Serilog;

namespace NumberSearch.DataAccess.PortControl
{
    public readonly record struct Number(string phoneNumber, string spId, string originalSpId, string providerName, string type, string status, string ocn, string npacRegion, DateTime activationDate, string lata, string lataName, string state, string rateCenter, string lrn, string billingId, string userLocationValue, string userLocationType, Networkdata networkData, Optionaldata optionalData)
    {
        /// <summary>
        /// Query the PortControl API for information on a dialed number. https://api.porting.com/swagger/index.html
        /// </summary>
        /// <param name="query">A dialed number.</param>
        /// <param name="accessToken">An access token for the PortControl API.</param>
        /// <returns>Information on the dialed number.</returns>
        public static async Task<Number> GetAsync(ReadOnlyMemory<char> tn, ReadOnlyMemory<char> accessToken)
        {
            ReadOnlySpan<char> baseUrl = "https://api.porting.com/";
            ReadOnlySpan<char> endpoint = "api/number/";

            string route = $"{baseUrl}{endpoint}{tn}";

            try
            {
                return await route.WithOAuthBearerToken(accessToken.ToString()).GetJsonAsync<Number>();
            }
            catch (FlurlHttpException ex)
            {
                var x = await ex.GetResponseStringAsync();
                Log.Warning(await ex.GetResponseStringAsync());
                var y = await ex.GetResponseJsonAsync<NumberError>();
                // Can't return this yet?
                return new();
            }
        }
    }

    public readonly record struct Networkdata(string classDpc, string classSsn, string lidbDpc, string lidbSsn, string isvmDpc, string isvmSsn, string cnamDpc, string cnamSsn, string wsmscDpc, string wsmscSsn);
    public readonly record struct Optionaldata(string altSpId, string lastAltSpId, string altBillingId, string altUserLocationType, string altUserLocationValue, string voiceUri, string mmsUri, string smsUri);

    // The errors from this endpoint are returned as JSON in the format below.
    public readonly record struct NumberError(Fielderrors fieldErrors, string result, string message);
    public readonly record struct NumberFielderrors(Tenant[] Tenant);
    public readonly record struct NumberTenant(string code, string message);
}
