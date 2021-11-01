using Flurl.Http;

using NumberSearch.DataAccess.TeliMessage;

using Serilog;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class EmergencyInfo
    {
        public int code { get; set; }
        public string status { get; set; }
        public EmergencyInfoDetail data { get; set; }
        public string error { get; set; }

        public class EmergencyInfoDetail
        {
            public string id { get; set; }
            public string did_id { get; set; }
            public string did_number { get; set; }
            public string full_name { get; set; }
            public string address { get; set; }
            public string city { get; set; }
            public string state { get; set; }
            public string zip { get; set; }
            public string unit_type { get; set; }
            public string unit_number { get; set; }
            public string create_dt { get; set; }
            public string modify_dt { get; set; }
            public string alert_group { get; set; }
            public string did_note { get; set; }
        }

        public static async Task<EmergencyInfo> GetAsync(string number, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "911/info";
            string tokenParameter = $"?token={token}";
            string numberParameter = $"&did_number={number}";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{numberParameter}";
            try
            {
                return await route.GetJsonAsync<EmergencyInfo>().ConfigureAwait(false);
            }
            catch (FlurlHttpException)
            {
                var result = await route.GetJsonAsync<TeliError>().ConfigureAwait(false);

                return new EmergencyInfo
                {
                    code = result.code,
                    status = result.status,
                    data = new EmergencyInfoDetail
                    {
                        did_note = result.data
                    }
                };
            }
        }

        public static async Task<TeliError> ValidateAddressAsync(string address, string city, string state, string zip, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "911/validate";
            string tokenParameter = $"?token={token}";
            string addressParameter = $"&address={address}";
            string cityParameter = $"&city={city}";
            string stateParameter = $"&state={state}";
            string zipParameter = $"&zip={zip}";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{addressParameter}{cityParameter}{stateParameter}{zipParameter}";

            try
            {
                return await route.GetJsonAsync<TeliError>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                var result = await ex.GetResponseStringAsync().ConfigureAwait(false);

                return new TeliError { code = 500, status = "error", data = result };
            }
        }

        public static async Task<EmergencyInfo> CreateE911RecordAsync(string didId, string fullName, string address, string city, string state, string zip, string unitType, string unitNumber, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "911/create";
            string tokenParameter = $"?token={token}";
            string didIdParameter = $"&did_id={didId}";
            string fullNameParameter = $"&full_name={fullName}";
            string addressParameter = $"&address={address}";
            string cityParameter = $"&city={city}";
            string stateParameter = $"&state={state}";
            string zipParameter = $"&zip={zip}";
            string unitTypeParameter = $"&unit_type={unitType}";
            string unitNumberParameter = $"&unit_number={unitNumber}";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{didIdParameter}{fullNameParameter}{addressParameter}{cityParameter}{stateParameter}{zipParameter}{unitTypeParameter}{unitNumberParameter}";

            try
            {
                return await route.GetJsonAsync<EmergencyInfo>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Fatal("[TeliMessage] [E911Registration] Failed to register E911 information.");
                Log.Fatal($"[TeliMessage] [E911Registration] {await ex.GetResponseStringAsync()}");
                return new EmergencyInfo { code = 500, error = await ex.GetResponseStringAsync() };
            }
        }
    }
}
