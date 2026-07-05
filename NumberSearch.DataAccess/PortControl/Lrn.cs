using Flurl.Http;

using Serilog;

namespace NumberSearch.DataAccess.PortControl
{

    /// <summary>
    /// LRN information from the PortControl API for a dialed number.
    /// </summary>
    /// <param name="npacRegions"></param>
    /// <param name="lrnId"></param>
    /// <param name="lrn"></param>
    /// <param name="spId"></param>
    /// <param name="lata"></param>
    /// <param name="blockCount"></param>
    /// <param name="numberCount"></param>
    /// <param name="creationDate"></param>
    public readonly record struct Lrn(string[] npacRegions, int lrnId, string lrn, string spId, string lata, int blockCount, int numberCount, DateTime creationDate)
    {
        /// <summary>
        /// Query the PortControl API for LRN information for a dialed number. https://api.porting.com/swagger/index.html
        /// </summary>
        /// <param name="query">A dialed number.</param>
        /// <param name="accessToken">An access token for the PortControl API.</param>
        /// <returns>The LRN information for the dialed number.</returns>
        public static async Task<Lrn> GetAsync(ReadOnlyMemory<char> query, ReadOnlyMemory<char> accessToken)
        {
            ReadOnlySpan<char> baseUrl = "https://api.porting.com/";
            ReadOnlySpan<char> endpoint = "api/lrn/";

            string route = $"{baseUrl}{endpoint}{query}";

            try
            {
                return await route.WithOAuthBearerToken(accessToken.ToString()).GetJsonAsync<Lrn>();
            }
            catch (FlurlHttpException ex)
            {
                var x = await ex.GetResponseStringAsync();
                Log.Warning(await ex.GetResponseStringAsync());
                var y = await ex.GetResponseJsonAsync<LrnError>();
                // Can't return this yet?
                return new();
            }
        }
    }

    // The errors from this endpoint are returned as JSON in the format below.
    public readonly record struct LrnError(Fielderrors fieldErrors, string result, string message);
    public readonly record struct Fielderrors(Tenant[] Tenant);
    public readonly record struct Tenant(string code, string message);
}
