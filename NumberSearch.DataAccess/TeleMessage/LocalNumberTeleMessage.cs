using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class LocalNumberTeleMessage
    {
        public int code { get; set; }
        public string status { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Mirrors the external API")]
        public TeleResults data { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Mirrors the external API")]
        public class TeleResults
        {
            public IEnumerable<Did> dids { get; set; }
            public int count { get; set; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Mirrors the external API")]
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

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Mirrors the external API")]
            public string setup_rate { get; set; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Mirrors the external API")]
            public string monthly_rate { get; set; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Mirrors the external API")]
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
                bool checkNpa = int.TryParse(item.npa, out int npa);
                bool checkNxx = int.TryParse(item.nxx, out int nxx);
                bool checkXxxx = int.TryParse(item.xxxx, out int xxxx);

                if (checkNpa && checkNxx && checkXxxx)
                {
                    list.Add(new PhoneNumber
                    {
                        NPA = npa,
                        NXX = nxx,
                        XXXX = xxxx,
                        DialedNumber = item.number,
                        City = item.ratecenter,
                        State = item.state,
                        IngestedFrom = "TeleMessage"
                    });
                }
            }

            return list;
        }
    }
}
