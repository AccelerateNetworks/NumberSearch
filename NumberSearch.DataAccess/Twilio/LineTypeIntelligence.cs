using Flurl.Http;

using Serilog;

using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Twilio
{

    public class LineTypeIntelligenceResponse
    {
        public object call_forwarding { get; set; }
        public object caller_name { get; set; }
        public string calling_country_code { get; set; }
        public string country_code { get; set; }
        public object disposable_phone_number_risk { get; set; }
        public object identity_match { get; set; }
        public LineTypeIntelligence line_type_intelligence { get; set; }
        public object live_activity { get; set; }
        public string national_format { get; set; }
        public string phone_number { get; set; }
        public object reassigned_number { get; set; }
        public object sim_swap { get; set; }
        public object sms_pumping_risk { get; set; }
        public string url { get; set; }
        public bool valid { get; set; }
        public object[] validation_errors { get; set; }

        public static async Task<LineTypeIntelligenceResponse> GetByDialedNumberAsync(string dialedNumber, string username, string password)
        {
            if(dialedNumber.Length is 10)
            {
                dialedNumber = $"1{dialedNumber}";
            }
            string baseUrl = "https://lookups.twilio.com/";
            string endpoint = "v2/PhoneNumbers/";
            string numberParameter = $"+{dialedNumber}?Fields=line_type_intelligence";
            string route = $"{baseUrl}{endpoint}{numberParameter}";
            try
            {
                var response = await route.WithBasicAuth(username, password).GetJsonAsync<LineTypeIntelligenceResponse>().ConfigureAwait(false);
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

    public class LineTypeIntelligence
    {
        public string carrier_name { get; set; }
        public object error_code { get; set; }
        public string mobile_country_code { get; set; }
        public string mobile_network_code { get; set; }
        public string type { get; set; }
    }
}