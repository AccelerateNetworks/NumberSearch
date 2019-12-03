using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Models
{
    public class LocalNumberTeleMessage
    {
        public int code { get; set; }
        public string status { get; set; }
        public Data data { get; set; }

        public class Data
        {
            public Did[] dids { get; set; }
            public int count { get; set; }
        }

        public class Did
        {
            public string id { get; set; }
            public string number { get; set; }
            public string npa { get; set; }
            public string nxx { get; set; }
            public string xxxx { get; set; }
            public string state { get; set; }
            public string ratecenter { get; set; }
            public string tier { get; set; }
            public string setup_rate { get; set; }
            public string monthly_rate { get; set; }
            public string per_minute_rate { get; set; }
        }

        public static async Task<LocalNumberTeleMessage> GetRawAsync(string query, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "dids/list";
            string tokenParameter = $"?token={token}";
            string searchParameter = $"&search={query}";
            string url = $"{baseUrl}{endpoint}{tokenParameter}{searchParameter}";
            return await url.GetJsonAsync<LocalNumberTeleMessage>();
        }

        public static async Task<IEnumerable<PhoneNumber>> GetAsync(string query, Guid token)
        {
            var results = await GetRawAsync(query, token);

            var list = new List<PhoneNumber>();

            foreach (var item in results.data.dids)
            {
                list.Add(new PhoneNumber
                {
                    NPA = item.npa,
                    NXX = item.nxx,
                    XXXX = item.xxxx,
                    DialedNumber = item.number,
                    City = item.ratecenter,
                    State = item.state,
                    IngestedFrom = "TeleMessage"
                });
            }

            return list;
        }
    }
}
