using Flurl.Http;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMessage
{
    public class DidsList
    {
        public int code { get; set; }
        public string status { get; set; } = string.Empty;
        public TeliResults data { get; set; } = new();

        public class TeliResults
        {
            public Did[] dids { get; set; } = Array.Empty<Did>();
            public int count { get; set; }
        }

        public class Did
        {
            public string id { get; set; } = string.Empty;
            public string number { get; set; } = string.Empty;
            public string npa { get; set; } = string.Empty;
            public string nxx { get; set; } = string.Empty;
            public string xxxx { get; set; } = string.Empty;
            public string state { get; set; } = string.Empty;
            public string ratecenter { get; set; } = string.Empty;
            public string tier { get; set; } = string.Empty;
            public string setup_rate { get; set; } = string.Empty;
            public string monthly_rate { get; set; } = string.Empty;
            public string per_minute_rate { get; set; } = string.Empty;
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

        public static async Task<DidsList> GetRawAsync(int npa, int nxx, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "dids/list";
            string tokenParameter = $"?token={token}";
            string npaParameter = $"&npa={npa}";
            string nxxParameter = $"&nxx={nxx}";
            string url = $"{baseUrl}{endpoint}{tokenParameter}{npaParameter}{nxxParameter}";
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

        public static async Task<PhoneNumber[]> GetAsync(int inNpa, Guid token)
        {
            var results = await GetRawAsync(inNpa, token).ConfigureAwait(false);

            var list = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results == null || results?.data == null || results?.data?.count == null || results?.data?.count < 1 || results?.data?.dids == null)
            {
                return Array.Empty<PhoneNumber>();
            }

            if (results?.data?.dids is not null)
            {
                foreach (var item in results.data.dids.ToArray())
                {
                    var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.number, out var phoneNumber);

                    if (checkParse && phoneNumber is not null)
                    {
                        list.Add(new PhoneNumber
                        {
                            NPA = phoneNumber.NPA,
                            NXX = phoneNumber.NXX,
                            XXXX = phoneNumber.XXXX,
                            DialedNumber = phoneNumber?.DialedNumber ?? string.Empty,
                            City = !string.IsNullOrWhiteSpace(item.ratecenter) ? item.ratecenter : "Unknown City",
                            State = !string.IsNullOrWhiteSpace(item.state) ? item.state : "Unknown State",
                            DateIngested = DateTime.Now,
                            IngestedFrom = "TeleMessage"
                        });
                    }
                }
            }

            return list.ToArray();
        }


        public static async Task<PhoneNumber[]> GetAsync(int npa, int nxx, Guid token)
        {
            var results = await GetRawAsync(npa, nxx, token).ConfigureAwait(false);

            var list = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results?.data?.count is null || results?.data?.count < 1 || results?.data?.dids is null)
            {
                return Array.Empty<PhoneNumber>();
            }

            if (results?.data?.dids is not null)
            {
                foreach (var item in results.data.dids.ToArray())
                {
                    var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.number, out var phoneNumber);

                    if (checkParse && phoneNumber is not null)
                    {
                        list.Add(new PhoneNumber
                        {
                            NPA = phoneNumber.NPA,
                            NXX = phoneNumber.NXX,
                            XXXX = phoneNumber.XXXX,
                            DialedNumber = phoneNumber?.DialedNumber ?? string.Empty,
                            City = !string.IsNullOrWhiteSpace(item.ratecenter) ? item.ratecenter : "Unknown City",
                            State = !string.IsNullOrWhiteSpace(item.state) ? item.state : "Unknown State",
                            DateIngested = DateTime.Now,
                            IngestedFrom = "TeleMessage"
                        });
                    }
                }
            }

            return list.ToArray();
        }

        public static async Task<PhoneNumber[]> GetAllTollfreeAsync(Guid token)
        {
            var results = await GetRawAllTollfreeAsync(token).ConfigureAwait(false);

            var list = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results?.data?.count is null || results?.data?.count < 1 || results?.data?.dids is null)
            {
                return Array.Empty<PhoneNumber>();
            }

            if (results?.data?.dids is not null)
            {
                foreach (var item in results.data.dids.ToArray())
                {
                    var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.number, out var phoneNumber);

                    if (checkParse && phoneNumber is not null)
                    {
                        list.Add(new PhoneNumber
                        {
                            NPA = phoneNumber.NPA,
                            NXX = phoneNumber.NXX,
                            XXXX = phoneNumber.XXXX,
                            DialedNumber = phoneNumber?.DialedNumber ?? string.Empty,
                            City = "Tollfree",
                            State = string.Empty,
                            DateIngested = DateTime.Now,
                            NumberType = "Tollfree",
                            IngestedFrom = "TeleMessage"
                        });
                    }
                }
            }

            return list.ToArray();
        }
    }
}
