using Flurl.Http;

using Newtonsoft.Json;

using Serilog;

using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.BulkVS
{
    public class E911Record
    {
        public string TN { get; set; }
        [JsonPropertyName("Caller Name")]
        [JsonProperty("Caller Name")]
        public bool CallerName { get; set; }
        [JsonPropertyName("Address Line 1")]
        [JsonProperty("Address Line 1")]
        public string AddressLine1 { get; set; }
        [JsonPropertyName("Address Line 2")]
        [JsonProperty("Address Line 2")]
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string[] Sms { get; set; }
        [JsonPropertyName("Last Modification")]
        [JsonProperty("Last Modification")]
        public DateTime LastModification { get; set; }

        public static async Task<E911Record> GetAsync(string dialedNumber, string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "e911Record";
            string numberParameter = $"?Number={dialedNumber}";
            string limitParameter = $"?Limit=100";
            string route = $"{baseUrl}{endpoint}{numberParameter}{limitParameter}";
            try
            {
                return await route.WithBasicAuth(username, password)
                    .GetJsonAsync<E911Record>()
                    .ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[E911] [BulkVS] No results found for number {dialedNumber}.");
                Log.Warning(await ex.GetResponseStringAsync());
                return null;
            }
        }

        public class ProvisionRequest
        {
            public string TN { get; set; }
            [JsonPropertyName("Caller Name")]
            [JsonProperty("Caller Name")]
            public string CallerName { get; set; }
            public string AddressID { get; set; }
            public string[] Sms { get; set; }
        }

        public class ProvisionResponse
        {
            public string Status { get; set; }
            public string TN { get; set; }
            [JsonPropertyName("Caller Name")]
            [JsonProperty("Caller Name")]
            public string CallerName { get; set; }
            [JsonPropertyName("Address Line 1")]
            [JsonProperty("Address Line 1")]
            public string AddressLine1 { get; set; }
            [JsonPropertyName("Address Line 2")]
            [JsonProperty("Address Line 2")]
            public string AddressLine2 { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string Zip { get; set; }
            public string[] Sms { get; set; }
            [JsonPropertyName("Last Modification")]
            [JsonProperty("Last Modification")]
            public DateTime LastModification { get; set; }
        }

        public static async Task<ProvisionResponse> PostAsync(string dialedNumber, string callerName, string addressId, string[] sms, string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "e911Record";
            string route = $"{baseUrl}{endpoint}";

            try
            {
                return await route.WithBasicAuth(username, password)
                    .PostJsonAsync(new ProvisionRequest { TN = dialedNumber, CallerName = callerName, AddressID = addressId, Sms = sms })
                    .ReceiveJson<ProvisionResponse>()
                    .ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[E911] [BulkVS] Unable to provision E911 service for {dialedNumber}.");
                Log.Warning(await ex.GetResponseStringAsync());
                return null;
            }
        }

        public class AddressToValidate
        {
            [JsonPropertyName("Street Number")]
            [JsonProperty("Street Number")]
            public string StreetNumber { get; set; }
            [JsonPropertyName("Street Name")]
            [JsonProperty("Street Name")]
            public string StreetName { get; set; }
            public string Location { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string Zip { get; set; }
        }

        public class ValidatedAddress
        {
            public string Status { get; set; }
            public string AddressID { get; set; }
            [JsonPropertyName("Address Line 1")]
            [JsonProperty("Address Line 1")]
            public string AddressLine1 { get; set; }
            [JsonPropertyName("Address Line 2")]
            [JsonProperty("Address Line 2")]
            public string AddressLine2 { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string Zip { get; set; }
        }

        public static async Task<ValidatedAddress> ValidateAddressAsync(string streetNumber, string streetName, string location, string city, string state, string zip, string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "validateAddress";
            string route = $"{baseUrl}{endpoint}";
            try
            {
                return await route.WithBasicAuth(username, password)
                    .PostJsonAsync(new AddressToValidate { StreetNumber = streetNumber, StreetName = streetName, Location = location, City = city, State = state, Zip = zip })
                    .ReceiveJson<ValidatedAddress>()
                    .ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[E911] [BulkVS] Failed to validate address for E911 service.");
                Log.Warning(await ex.GetResponseStringAsync());
                return null;
            }
        }
    }
}