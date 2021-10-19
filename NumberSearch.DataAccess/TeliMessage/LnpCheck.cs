using Flurl.Http;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMesssage
{
    // TODO: This is a mess. We need to clean this up and correctly model the API interface.
    public class LnpCheck
    {
        public int code { get; set; }
        public string status { get; set; }

        [JsonExtensionData]
        public Dictionary<string, LnpResult> data { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Mirrors the external API")]
        public class LnpResult
        {
            public string status { get; set; }
            public string message { get; set; }
        }

        public class Portability
        {
            public string dialedNumber { get; set; }
            public bool Portable { get; set; }
        }

        public static async Task<LnpCheck> GetRawAsync(string query, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "lnp/check";
            string tokenParameter = $"?token={token}";
            string searchParameter = $"&numbers={query}";
            string url = $"{baseUrl}{endpoint}{tokenParameter}{searchParameter}";
            return await url.GetJsonAsync<LnpCheck>();
        }

        public static async Task<LnpCheck> GetRawInBulkAsync(string[] numbers, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "lnp/check/portable/bulk";
            string tokenParameter = $"?token={token}";
            string searchParameter = $"&numbers=[{string.Join(",", numbers)}]";
            string url = $"{baseUrl}{endpoint}{tokenParameter}{searchParameter}";
            return await url.GetJsonAsync<LnpCheck>();
        }

        public static async Task<bool> IsPortableAsync(string query, Guid token)
        {
            var results = await GetRawAsync(query, token).ConfigureAwait(false);

            var checkValue = results.data.TryGetValue(query, out var value);

            if (checkValue)
            {
                return value.status == "success";
            }
            else
            {
                return true;
            }
        }

        public static async Task<Portability[]> IsBulkPortableAsync(string[] numbers, Guid token)
        {
            var results = await GetRawInBulkAsync(numbers, token).ConfigureAwait(false);

            var numbersAndPortability = new List<Portability>();

            foreach (var number in numbers)
            {
                var checkValue = results.data.TryGetValue(number, out var value);

                if (checkValue)
                {
                    if (value.status == "success")
                    {
                        numbersAndPortability.Add(new Portability { dialedNumber = number, Portable = true });
                    }
                    else
                    {
                        numbersAndPortability.Add(new Portability { dialedNumber = number, Portable = false });
                    }
                }
                else
                {
                    numbersAndPortability.Add(new Portability { dialedNumber = number, Portable = false });
                }
            }

            return numbersAndPortability.ToArray();
        }
    }
}