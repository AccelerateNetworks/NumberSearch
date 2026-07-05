using Flurl.Http;

using Serilog;

namespace NumberSearch.DataAccess.PortControl
{
    public readonly record struct AccountInfo(string userId, string companyId, string userName, string email,
        string firstName, string lastName, string locale, string zoneInfo, Settings settings, string[] permissions,
        Spidprofile[] spIdProfiles, string supportCode, bool isAdminProjectIdAccessible, string clientType,
        bool showFeedbackPopUp)
    {
        public static async Task<AccountInfo> GetAsync(ReadOnlyMemory<char> accessToken)
        {
            ReadOnlySpan<char> baseUrl = "https://api.porting.com/";
            ReadOnlySpan<char> endpoint = "api/account/info";

            string route = $"{baseUrl}{endpoint}";

            try
            {
                return await route.WithOAuthBearerToken(accessToken.ToString()).GetJsonAsync<AccountInfo>();
            }
            catch (FlurlHttpException ex)
            {
                var x = await ex.GetResponseStringAsync();
                Log.Warning(await ex.GetResponseStringAsync());
                return new();
            }
        }
    }

    public readonly record struct Settings(string ui);

    public readonly record struct Spidprofile(string spId, string name, string companyId, string internalId, bool external,
        bool nonSpIdCompany, bool allowCsr, string status, string altSpid);

}
