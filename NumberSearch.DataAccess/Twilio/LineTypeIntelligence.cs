using Flurl.Http;

using Serilog;

using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Twilio
{

    public class LineTypeIntelligenceResponse
    {
        public object call_forwarding { get; set; } = new();
        public object caller_name { get; set; } = new();
        public string calling_country_code { get; set; } = string.Empty;
        public string country_code { get; set; } = string.Empty;
        public object disposable_phone_number_risk { get; set; } = new();
        public object identity_match { get; set; } = new();
        public LineTypeIntelligence line_type_intelligence { get; set; } = new();
        public object live_activity { get; set; } = new();
        public string national_format { get; set; } = string.Empty;
        public string phone_number { get; set; } = string.Empty;
        public object reassigned_number { get; set; } = new();
        public object sim_swap { get; set; } = new();
        public object sms_pumping_risk { get; set; } = new();
        public string url { get; set; } = string.Empty;
        public bool valid { get; set; }
        public object[] validation_errors { get; set; } = [];

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
        public string carrier_name { get; set; } = string.Empty;
        public object error_code { get; set; } = new();
        public string mobile_country_code { get; set; } = string.Empty;
        public string mobile_network_code { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
    }
}