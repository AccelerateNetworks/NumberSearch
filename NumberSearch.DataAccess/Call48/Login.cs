using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Call48
{
    public class Login
    {

        public string error { get; set; }
        public Data data { get; set; }
        public int code { get; set; }

        public class Data
        {
            public string token { get; set; }
        }

        /// <summary>
        /// Get a valid security token from Call48.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static async Task<Login> LoginAsync(string username, string password)
        {
            string baseUrl = "https://apicontrol.call48.com/api/v4/";
            string endPointName = $"login";

            string route = $"{baseUrl}{endPointName}";

            return await route.PostJsonAsync(new { user_name = username, password }).ReceiveJson<Login>().ConfigureAwait(false);
        }
    }
}
