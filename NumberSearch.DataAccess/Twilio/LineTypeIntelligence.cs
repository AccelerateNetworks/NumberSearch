using Flurl.Http;

using Serilog;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Twilio
{

    public readonly record struct LineTypeIntelligenceResponse
    (
        object call_forwarding,
        object caller_name,
        string calling_country_code,
        string country_code,
        object disposable_phone_number_risk,
        object identity_match,
        LineTypeIntelligence line_type_intelligence,
        object live_activity,
        string national_format,
        string phone_number,
        object reassigned_number,
        object sim_swap,
        object sms_pumping_risk,
        string url,
        bool valid,
        object[] validation_errors)
    {
        public static async Task<LineTypeIntelligenceResponse> GetByDialedNumberAsync(ReadOnlyMemory<char> dialedNumber, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            string numberParameter;
            if (dialedNumber.Length is 10)
            {
                numberParameter = $"+1{dialedNumber}?Fields=line_type_intelligence";
            }
            else
            {
                numberParameter = $"+{dialedNumber}?Fields=line_type_intelligence";
            }
            string baseUrl = "https://lookups.twilio.com/";
            string endpoint = "v2/PhoneNumbers/";
            string route = $"{baseUrl}{endpoint}{numberParameter}";
            try
            {
                var response = await route.WithBasicAuth(username.ToString(), password.ToString()).GetJsonAsync<LineTypeIntelligenceResponse>();
                return response;
            }
            catch (FlurlHttpException ex)
            {
                Log.Warning($"[Portability] [BulkVS] No results found for number {dialedNumber}.");
                Log.Warning(await ex.GetResponseStringAsync());
                return new();
            }
        }
    }

    public readonly record struct LineTypeIntelligence
    (
        string carrier_name,
        object error_code,
        string mobile_country_code,
        string mobile_network_code,
        string type
    );
}