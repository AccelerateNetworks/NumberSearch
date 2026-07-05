using Flurl.Http;

using Serilog;

namespace NumberSearch.DataAccess.PortControl
{

    public readonly record struct AuthRequest(string username, string password, string client_id, string client_secret, string grant_type = "password", string scope = "portcontrol.web.api.tst")
    {
        /// <summary>
        /// Get an auth token for the PortControl API. https://api.porting.com/swagger/index.html
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="client_id"></param>
        /// <param name="client_secret"></param>
        /// <param name="grant_type"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        public async Task<AuthResponse> PostAsync()
        {
            ReadOnlySpan<char> baseUrl = "https://auth.porting.com/";
            ReadOnlySpan<char> endpoint = "connect/token";

            string route = $"{baseUrl}{endpoint}";

            try
            {
                return await route.PostUrlEncodedAsync(this).ReceiveJson<AuthResponse>();
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning(await ex.GetResponseStringAsync());
                return new();
            }
        }
    };

    public readonly record struct AuthResponse(string access_token, int expires_in, string token_type, string scope);
}
