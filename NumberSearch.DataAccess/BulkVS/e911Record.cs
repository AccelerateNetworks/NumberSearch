using Flurl.Http;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.BulkVS
{
    public readonly record struct E911Record
    (
        string TN,
        [property: JsonPropertyName("Caller Name")]
        string CallerName,
        [property: JsonPropertyName("Address Line 1")]
        string AddressLine1,
        [property: JsonPropertyName("Address Line 2")]
        string AddressLine2,
        string City,
        string State,
        string Zip,
        //string[] Sms,
        [property: JsonPropertyName("Last Modification")]
        string LastModification
    )
    {
        public static async Task<E911Record[]> GetAsync(ReadOnlyMemory<char> dialedNumber, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "e911Record";
            string numberParameter = $"?Number={dialedNumber}";
            string limitParameter = $"&Limit=100";
            string route = $"{baseUrl}{endpoint}{numberParameter}{limitParameter}";
            try
            {
                var results = await route.WithBasicAuth(username.ToString(), password.ToString()).GetJsonAsync<E911Record[]>();
                return results;
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning("[E911] [BulkVS] No results found for number {dialedNumber}.", dialedNumber);
                var response = await ex.GetResponseStringAsync();
                Log.Warning(response);
                return [];
            }
        }

        public static async Task<E911Record[]> GetAllAsync(ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "e911Record";
            string limitParameter = $"?Limit=9999";
            string route = $"{baseUrl}{endpoint}{limitParameter}";
            try
            {
                return await route.WithBasicAuth(username.ToString(), password.ToString())
                    .GetJsonAsync<E911Record[]>();
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning("[E911] [BulkVS] No results found.");
                Log.Warning(await ex.GetResponseStringAsync());
                return [];
            }
        }

        public readonly record struct ProvisionRequest
        (
            string TN,
            [property: JsonPropertyName("Caller Name")]
            string CallerName,
            string AddressID,
            string[] Sms
        );

        public readonly record struct ProvisionResponse
        (
            string Status,
            string TN,
            [property: JsonPropertyName("Caller Name")]
            string CallerName,
            [property: JsonPropertyName("Address Line 1")]
            string AddressLine1,
            [property: JsonPropertyName("Address Line 2")]
            string AddressLine2,
            string City,
            string State,
            string Zip,
            string[] Sms,
            // Only exists to make parsing the JSON easier.
            [property: JsonPropertyName("Last Modification")]
            string UnparsedLastModDate,
            DateTime LastModification
        );

        public static async Task<ProvisionResponse> PostAsync(string dialedNumber, string callerName, string addressId, string[] sms, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "e911Record";
            string route = $"{baseUrl}{endpoint}";

            try
            {
                var response = await route.WithBasicAuth(username.ToString(), password.ToString())
                .PostJsonAsync(new ProvisionRequest { TN = dialedNumber, CallerName = callerName, AddressID = addressId, Sms = sms })
                .ReceiveJson<ProvisionResponse>();

                bool checkParse = DateTime.TryParse(response.UnparsedLastModDate, out DateTime parsed);

                response = response with { LastModification = checkParse ? parsed : DateTime.Now };

                return response;
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning("[E911] [BulkVS] Unable to provision E911 service for {dialedNumber}.", dialedNumber);
                var x = await ex.GetResponseStringAsync();
                Log.Warning(x);
                return new();
            }
        }

        public readonly record struct AddressToValidate
        (
            [property: JsonPropertyName("Street Number")]
            string StreetNumber,
            [property: JsonPropertyName("Street Name")]
            string StreetName,
            string Location,
            string City,
            string State,
            string Zip
        );

        public readonly record struct ValidatedAddress
        (
            string Status,
            string AddressID,
            [property: JsonPropertyName("Address Line 1")]
            string AddressLine1,
            [property: JsonPropertyName("Address Line 2")]
            string AddressLine2,
            string City,
            string State,
            string Zip
        );

        public static async Task<ValidatedAddress> ValidateAddressAsync(string streetNumber, string streetName, string location, string city, string state, string zip, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            string baseUrl = "https://portal.bulkvs.com/api/v1.0/";
            string endpoint = "validateAddress";
            string route = $"{baseUrl}{endpoint}";

            try
            {
                return await route.WithBasicAuth(username.ToString(), password.ToString())
                    .PostJsonAsync(new AddressToValidate { StreetNumber = streetNumber, StreetName = streetName, Location = location, City = city, State = state, Zip = zip })
                    .ReceiveJson<ValidatedAddress>();
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[E911] [BulkVS] Failed to validate address for E911 service.");
                var response = await ex.GetResponseJsonAsync<ValidatedAddress>();
                return response;
            }
        }
    }
}