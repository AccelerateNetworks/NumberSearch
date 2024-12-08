using Flurl.Http;

using Newtonsoft.Json;

using NumberSearch.DataAccess.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.BulkVS
{
    public readonly record struct TnRecord
    (
        string TN,
        string Status,
        string Lidb,
        [property: JsonPropertyName("Portout Pin")]
        [property: JsonProperty("Portout Pin")]
        string PortoutPin,
        TnRecord.TnRecordRouting Routing,
        TnRecord.TnRecordMessaging Messaging,
        [property: JsonPropertyName("TN Details")]
        [property: JsonProperty("TN Details")]
        TnRecord.TnRecordTNDetails TNDetails
    )
    {

        public readonly record struct TnRecordRouting
        (
            [property: JsonPropertyName("Trunk Group")]
            [property: JsonProperty("Trunk Group")]
            string TrunkGroup,
            [property: JsonPropertyName("Custom URI")]
            [property: JsonProperty("Custom URI")]
            string CustomURI,
            [property: JsonPropertyName("Call Forward")]
            [property: JsonProperty("Call Forward")]
            object CallForward);


        public readonly record struct TnRecordMessaging(bool Sms, bool Mms);

        public readonly record struct TnRecordTNDetails(
            [property: JsonPropertyName("Rate Center")]
            [property: JsonProperty("Rate Center")]
            string RateCenter,
            string State,
            string Tier,
            bool Cnam,
            [property: JsonPropertyName("Activation Date")]
            [property: JsonProperty("Activation Date")]
            string ActivationDate);

        public static async Task<TnRecord[]> GetRawAsync(ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "tnRecord";
            string route = $"{baseUrl}{endpoint}";
            try
            {
                return await route.WithBasicAuth(username.ToString(), password.ToString()).GetJsonAsync<TnRecord[]>();
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning("[Ingest] [OwnedNumbers] [BulkVS] No results found.");
                Log.Warning(await ex.GetResponseStringAsync());
                return [];
            }
        }

        public static async Task<TnRecord> GetByDialedNumberAsync(ReadOnlyMemory<char> dialedNumber, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "tnRecord";
            string numberParameter = $"?Number=1{dialedNumber}";
            string route = $"{baseUrl}{endpoint}{numberParameter}";
            try
            {
                var results = await route.WithBasicAuth(username.ToString(), password.ToString()).GetJsonAsync<TnRecord[]>();
                return results.FirstOrDefault();
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning("[Ingest] [OwnedNumbers] [BulkVS] No results found.");
                Log.Warning(await ex.GetResponseStringAsync());
                return new();
            }
        }

        public static async Task<PhoneNumber[]> GetAsync(ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            var results = await GetRawAsync(username, password);
            var output = new List<PhoneNumber>();

            // Bail out early if something is wrong.
            if (results is null || results.Length == 0)
            {
                return [];
            }

            foreach (var item in results)
            {

                var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.TN, out var phoneNumber);

                if (checkParse)
                {
                    output.Add(new PhoneNumber
                    {
                        NPA = phoneNumber.NPA,
                        NXX = phoneNumber.NXX,
                        XXXX = phoneNumber.XXXX,
                        DialedNumber = phoneNumber.DialedNumber ?? string.Empty,
                        City = string.IsNullOrWhiteSpace(item.TNDetails.RateCenter) ? "Unknown City" : item.TNDetails.RateCenter,
                        State = string.IsNullOrWhiteSpace(item.TNDetails.State) ? "Unknown State" : item.TNDetails.State,
                        DateIngested = DateTime.Now,
                        IngestedFrom = "BulkVS"
                    });
                }
                else
                {
                    Log.Fatal("[Ingest] [BulkVS] Failed to parse {TN}", item.TN);
                }
            }
            return [.. output];
        }

        public static async Task<OwnedPhoneNumber[]> GetOwnedAsync(ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            var results = await GetRawAsync(username, password);
            var output = new List<OwnedPhoneNumber>();

            // Bail out early if something is wrong.
            if (results is null || results.Length == 0)
            {
                return [];
            }

            foreach (var item in results)
            {
                var checkParsed = PhoneNumbersNA.PhoneNumber.TryParse(item.TN, out var phoneNumber);

                if (checkParsed)
                {
                    output.Add(new OwnedPhoneNumber
                    {
                        DialedNumber = phoneNumber.DialedNumber ?? string.Empty,
                        IngestedFrom = "BulkVS",
                        TrunkGroup = item.Routing.TrunkGroup ?? string.Empty,
                        Active = true,
                        DateIngested = DateTime.Now
                    });
                }
                else
                {
                    Log.Fatal("[Ingest] [BulkVS] Failed to parse {TN}", item.TN);
                }
            }
            return [.. output];
        }
    }
}