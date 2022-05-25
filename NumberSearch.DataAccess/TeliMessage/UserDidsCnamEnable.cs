using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMessage
{
    // TODO: This is a mess. We need to clean this up and correctly model the API interface.
    public class UserDidsCnamEnable
    {
        public int code { get; set; }
        public string status { get; set; }
        public string data { get; set; }

        /// <summary>
        /// Enable CNAM on this number.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<UserDidsCnamEnable> GetAsync(string dialedNumber, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "user/dids/cnam/enable";
            string tokenParameter = $"?token={token}";
            string numberParameter = $"&number={dialedNumber}";
            string url = $"{baseUrl}{endpoint}{tokenParameter}{numberParameter}";
            return await url.GetJsonAsync<UserDidsCnamEnable>().ConfigureAwait(false);
        }

        /// <summary>
        /// Is CNAM enabled for this number?
        /// </summary>
        /// <returns></returns>
        public bool CnamEnabled()
        {
            return status == "success";
        }
    }
}