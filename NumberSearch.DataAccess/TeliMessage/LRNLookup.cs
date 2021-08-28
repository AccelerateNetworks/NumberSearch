using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class LrnLookup
    {
        public int code { get; set; }
        public string status { get; set; }
        public TeleLRNResult data { get; set; }
        public string LIDBName { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Mirrors the external API")]
        public class TeleLRNResult
        {
            public string DialedNumber { get; set; }
            public string status { get; set; }
            public string spid { get; set; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Mirrors the external API")]
            public string total_ported_spid { get; set; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Mirrors the external API")]
            public string port_date { get; set; }
            public string ratecenter { get; set; }
            public string state { get; set; }
            public string lata { get; set; }
            public string clli { get; set; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "<Pending>")]
            public string creation_date { get; set; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "<Pending>")]
            public string fraud_risk { get; set; }
            public string lrn { get; set; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "<Pending>")]
            public string spid_name { get; set; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "<Pending>")]
            public string ocn_name { get; set; }
            public string ocn { get; set; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "<Pending>")]
            public string ocn_type { get; set; }
        }

        public static async Task<LrnLookup> GetAsync(string number, Guid token)
        {
            string baseUrl = "https://lrn.teleapi.net/";
            string endpoint = "lookup";
            string tokenParameter = $"?token={token}";
            string numberParameter = $"&number={number}";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{numberParameter}";
            return await route.GetJsonAsync<LrnLookup>().ConfigureAwait(false);
        }
    }
}
