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
        public string TN { get; set; } = string.Empty;
        [JsonPropertyName("Caller Name")]
        [JsonProperty("Caller Name")]
        public string CallerName { get; set; }
        [JsonPropertyName("Address Line 1")]
        [JsonProperty("Address Line 1")]
        public string AddressLine1 { get; set; } = string.Empty;
        [JsonPropertyName("Address Line 2")]
        [JsonProperty("Address Line 2")]
        public string AddressLine2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string[] Sms { get; set; } = Array.Empty<string>();
        [JsonPropertyName("Last Modification")]
        [JsonProperty("Last Modification")]
        public DateTime LastModification { get; set; }

        public static async Task<E911Record[]> GetAsync(string dialedNumber, string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "e911Record";
            string numberParameter = $"?Number={dialedNumber}";
            string limitParameter = $"&Limit=100";
            string route = $"{baseUrl}{endpoint}{numberParameter}{limitParameter}";
            try
            {
                return await route.WithBasicAuth(username, password)
                    .GetJsonAsync<E911Record[]>()
                    .ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[E911] [BulkVS] No results found for number {dialedNumber}.");
                var response = await ex.GetResponseStringAsync();
                Log.Warning(await ex.GetResponseStringAsync());
                return Array.Empty<E911Record>();
            }
        }

        public static async Task<E911Record[]> GetAllAsync(string username, string password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "e911Record";
            string limitParameter = $"?Limit=9999";
            string route = $"{baseUrl}{endpoint}{limitParameter}";
            try
            {
                return await route.WithBasicAuth(username, password)
                    .GetJsonAsync<E911Record[]>()
                    .ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[E911] [BulkVS] No results found.");
                Log.Warning(await ex.GetResponseStringAsync());
                return Array.Empty<E911Record>();
            }
        }

        public class ProvisionRequest
        {
            public string TN { get; set; } = string.Empty;
            [JsonPropertyName("Caller Name")]
            [JsonProperty("Caller Name")]
            public string CallerName { get; set; } = string.Empty;
            public string AddressID { get; set; } = string.Empty;
            public string[] Sms { get; set; } = Array.Empty<string>();
        }

        public class ProvisionResponse
        {
            public string Status { get; set; } = string.Empty;
            public string TN { get; set; } = string.Empty;
            [JsonPropertyName("Caller Name")]
            [JsonProperty("Caller Name")]
            public string CallerName { get; set; } = string.Empty;
            [JsonPropertyName("Address Line 1")]
            [JsonProperty("Address Line 1")]
            public string AddressLine1 { get; set; } = string.Empty;
            [JsonPropertyName("Address Line 2")]
            [JsonProperty("Address Line 2")]
            public string AddressLine2 { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string State { get; set; } = string.Empty;
            public string Zip { get; set; } = string.Empty;
            public string[] Sms { get; set; } = Array.Empty<string>();
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
                return new();
            }
        }

        public class AddressToValidate
        {
            [JsonPropertyName("Street Number")]
            [JsonProperty("Street Number")]
            public string StreetNumber { get; set; } = string.Empty;
            [JsonPropertyName("Street Name")]
            [JsonProperty("Street Name")]
            public string StreetName { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string State { get; set; } = string.Empty;
            public string Zip { get; set; } = string.Empty;
        }

        public class ValidatedAddress
        {
            public string Status { get; set; } = string.Empty;
            public string AddressID { get; set; } = string.Empty;
            [JsonPropertyName("Address Line 1")]
            [JsonProperty("Address Line 1")]
            public string AddressLine1 { get; set; } = string.Empty;
            [JsonPropertyName("Address Line 2")]
            [JsonProperty("Address Line 2")]
            public string AddressLine2 { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string State { get; set; } = string.Empty;
            public string Zip { get; set; } = string.Empty;
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
                return new();
            }
        }
    }
}