using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Call48
{
    public class Login
    {

        public object error { get; set; }
        public Data data { get; set; }
        public int code { get; set; }

        public class Data
        {
            public string token { get; set; }
        }

        /// <summary>
        /// Get LRN lookup information for a specific dialed number.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static async Task<Login> GetAsync(string username, string password)
        {
            string baseUrl = "https://apicontrol.call48.com/api/v4/";
            string endPointName = $"login";

            string route = $"{baseUrl}{endPointName}";

            try
            {
                var result = await route.PostJsonAsync(new { usern_name = username, password }).ReceiveJson<Login>().ConfigureAwait(false);

                return result;
            }
            catch (FlurlHttpException ex)
            {
                throw;
            }
        }
    }
}
