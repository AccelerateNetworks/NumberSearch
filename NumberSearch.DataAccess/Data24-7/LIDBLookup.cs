using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Data247
{
    public class LIDBLookup
    {
        public Response response { get; set; } = new();

        public class Response
        {
            public Result[] results { get; set; } = Array.Empty<Result>();
        }

        public class Result
        {
            public string status { get; set; } = string.Empty;
            public string number { get; set; } = string.Empty;
            public string name { get; set; } = string.Empty;
        }

        public static async Task<LIDBLookup> GetAsync(string number, string username, string password)
        {
            string baseUrl = "https://api.data24-7.com/v/2.0";
            string reponseTypeParameter = $"?out=json";
            string usernameParameter = $"&user={username}";
            string passwordParameter = $"&pass={password}";
            string apiParameter = $"&api=I";
            string numberParameter = $"&p1={number}";
            string route = $"{baseUrl}{reponseTypeParameter}{usernameParameter}{passwordParameter}{apiParameter}{numberParameter}";

            return await route.GetJsonAsync<LIDBLookup>().ConfigureAwait(false);
        }
    }
}
