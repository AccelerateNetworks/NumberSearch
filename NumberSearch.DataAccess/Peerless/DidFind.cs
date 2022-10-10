using Flurl.Http;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Peerless
{

    public class DidFind
    {
        public string did { get; set; }
        public string category { get; set; }

        public static async Task<IEnumerable<DidFind>> GetRawAsync(string npa, string apiKey)
        {
            string baseUrl = "https://api.peerlessnetwork.io/mag/v1/";
            string endpoint = "did/find";
            string npaParameter = $"?npa={npa}";
            string apiKeyParameter = $"&api_key={apiKey}";
            string route = $"{baseUrl}{endpoint}{npaParameter}{apiKeyParameter}";
            return await route.GetJsonAsync<IEnumerable<DidFind>>().ConfigureAwait(false);
        }

        public static async Task<IEnumerable<DidFind>> GetRawByRateCenterAsync(string ratecenter, string apiKey)
        {
            string baseUrl = "https://api.peerlessnetwork.io/mag/v1/";
            string endpoint = "did/find";
            string ratecenterParameter = $"?rate_center={ratecenter}";
            string apiKeyParameter = $"&api_key={apiKey}";
            string route = $"{baseUrl}{endpoint}{ratecenterParameter}{apiKeyParameter}";
            return await route.GetJsonAsync<IEnumerable<DidFind>>().ConfigureAwait(false);
        }

        public static async Task<IEnumerable<DidFind>> GetRawAsync(string npa, string nxx, string xxxx, string apiKey)
        {
            string baseUrl = "https://api.peerlessnetwork.io/mag/v1/";
            string endpoint = "did/find";
            string npaParameter = $"?npa={npa}";
            string nxxParameter = $"&nxx={nxx}";
            string xxxxParameter = $"&xxxx={xxxx}";
            string apiKeyParameter = $"&api_key={apiKey}";
            string route = $"{baseUrl}{endpoint}{npaParameter}{nxxParameter}{xxxxParameter}{apiKeyParameter}";
            return await route.GetJsonAsync<IEnumerable<DidFind>>().ConfigureAwait(false);
        }

        public static async Task<IEnumerable<PhoneNumber>> GetByNPAAsync(string npa, string apiKey)
        {
            var results = await GetRawAsync(npa, apiKey).ConfigureAwait(false);

            var list = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results == null || !results.Any())
            {
                return list;
            }

            foreach (var item in results)
            {
                var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.did, out var phoneNumber);

                if (checkParse)
                {
                    list.Add(new PhoneNumber
                    {
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        DialedNumber = phoneNumber.DialedNumber,
                        City = "Unknown City",
                        State = "Unknown State",
                        DateIngested = DateTime.Now,
                        IngestedFrom = "Peerless"
                    });
                }
            }
            return list.ToArray();
        }

        public static async Task<IEnumerable<PhoneNumber>> GetByRateCenterAsync(string ratecenter, string apiKey)
        {
            var results = await GetRawByRateCenterAsync(ratecenter, apiKey).ConfigureAwait(false);

            var list = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results == null || !results.Any())
            {
                return list;
            }

            foreach (var item in results)
            {
                var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.did, out var phoneNumber);

                if (checkParse)
                {
                    list.Add(new PhoneNumber
                    {
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        DialedNumber = phoneNumber.DialedNumber,
                        City = "Unknown City",
                        State = "Unknown State",
                        DateIngested = DateTime.Now,
                        IngestedFrom = "Peerless"
                    });
                }
            }
            return list.ToArray();
        }

        public static async Task<IEnumerable<PhoneNumber>> GetByDialedNumberAsync(string npa, string nxx, string xxxx, string apiKey)
        {
            var results = await GetRawAsync(npa, nxx, xxxx, apiKey).ConfigureAwait(false);

            var list = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results == null || !results.Any())
            {
                return list;
            }

            foreach (var item in results)
            {
                var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.did, out var phoneNumber);

                if (checkParse)
                {
                    list.Add(new PhoneNumber
                    {
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        DialedNumber = phoneNumber.DialedNumber,
                        City = "Unknown City",
                        State = "Unknown State",
                        DateIngested = DateTime.Now,
                        IngestedFrom = "Peerless"
                    });
                }
            }
            return list.ToArray();
        }
    }
}