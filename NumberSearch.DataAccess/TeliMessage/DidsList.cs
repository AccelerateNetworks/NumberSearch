﻿using Flurl.Http;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeleMesssage
{
    public class DidsList
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

        public static async Task<DidsList> GetRawAsync(string query, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "dids/list";
            string tokenParameter = $"?token={token}";
            string searchParameter = $"&search={query}";
            string url = $"{baseUrl}{endpoint}{tokenParameter}{searchParameter}";
            return await url.GetJsonAsync<DidsList>().ConfigureAwait(false);
        }

        public static async Task<DidsList> GetRawAsync(int npa, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "dids/list";
            string tokenParameter = $"?token={token}";
            string npaParameter = $"&npa={npa}";
            string url = $"{baseUrl}{endpoint}{tokenParameter}{npaParameter}";
            return await url.GetJsonAsync<DidsList>().ConfigureAwait(false);
        }

        public static async Task<DidsList> GetRawAllTollfreeAsync(Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "dids/list";
            string tokenParameter = $"?token={token}";
            string tollfreeParameter = $"&type=tollfree";
            string url = $"{baseUrl}{endpoint}{tokenParameter}{tollfreeParameter}";
            return await url.GetJsonAsync<DidsList>().ConfigureAwait(false);
        }

        public static async Task<IEnumerable<PhoneNumber>> GetAsync(int inNpa, Guid token)
        {
            var results = await GetRawAsync(inNpa, token).ConfigureAwait(false);

            var list = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results == null || results?.data == null || results?.data?.count == null || results?.data?.count < 1 || results?.data?.dids == null)
            {
                return list;
            }

            foreach (var item in results?.data?.dids?.ToArray())
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
                        City = !string.IsNullOrWhiteSpace(item.ratecenter) ? item.ratecenter : "Unknown City",
                        State = !string.IsNullOrWhiteSpace(item.state) ? item.state : "Unknown State",
                        DateIngested = DateTime.Now,
                        IngestedFrom = "TeleMessage"
                    });
                }
            }

            return list;
        }


        public static async Task<IEnumerable<PhoneNumber>> GetAsync(string query, Guid token)
        {
            var results = await GetRawAsync(query, token).ConfigureAwait(false);

            var list = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results == null || results?.data == null || results?.data?.count == null || results?.data?.count < 1 || results?.data?.dids == null)
            {
                return list;
            }

            foreach (var item in results?.data?.dids?.ToArray())
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
                        City = !string.IsNullOrWhiteSpace(item.ratecenter) ? item.ratecenter : "Unknown City",
                        State = !string.IsNullOrWhiteSpace(item.state) ? item.state : "Unknown State",
                        DateIngested = DateTime.Now,
                        IngestedFrom = "TeleMessage"
                    });
                }
            }

            return list;
        }

        public static async Task<IEnumerable<PhoneNumber>> GetAllTollfreeAsync(Guid token)
        {
            var results = await GetRawAllTollfreeAsync(token).ConfigureAwait(false);

            var list = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results == null || results?.data == null || results?.data?.count == null || results?.data?.count < 1 || results?.data?.dids == null)
            {
                return list;
            }

            foreach (var item in results?.data?.dids?.ToArray())
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
                        City = "Tollfree",
                        State = string.Empty,
                        DateIngested = DateTime.Now,
                        NumberType = "Tollfree",
                        IngestedFrom = "TeleMessage"
                    });
                }
            }

            return list;
        }
    }
}