using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMessage
{
    public class UserDidsLibdGet
    {
        public int code { get; set; }
        public string status { get; set; } = string.Empty;
        public string data { get; set; } = string.Empty;

        /// <summary>
        /// Enable CNAM on this number.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<UserDidsLibdGet> GetAsync(string did_id, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "user/dids/lidb/get";
            string tokenParameter = $"?token={token}";
            string didIdParameter = $"&did_id={did_id}";
            string url = $"{baseUrl}{endpoint}{tokenParameter}{didIdParameter}";
            return await url.GetJsonAsync<UserDidsLibdGet>().ConfigureAwait(false);
        }
    }
}