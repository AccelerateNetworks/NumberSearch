using Flurl.Http;

using Serilog;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMessage
{
    public class DidsOffnet
    {
        public int code { get; set; }
        public string status { get; set; }
        public string error { get; set; }
        public OffnetResponseData data { get; set; }

        public class OffnetResponseData
        {
            public string jobid { get; set; }
        }

        public class StatusResponse
        {
            public int code { get; set; }
            public string status { get; set; }
            public string data { get; set; }
        }

        public static async Task<DidsOffnet> VerifyCapabilityAsync(string number, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "dids/offnet/check";
            string tokenParameter = $"?token={token}";
            string numbersParameter = $"&numbers={number}";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{numbersParameter}";

            try
            {
                return await route.GetJsonAsync<DidsOffnet>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Fatal($"{await ex.GetResponseStringAsync()}");
                return new DidsOffnet { code = 500, status = "error", error = await ex.GetResponseJsonAsync() };
            }
        }

        public static async Task<DidsOffnet> SubmitNumberAsync(string number, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "dids/offnet/submit";
            string tokenParameter = $"?token={token}";
            string numbersParameter = $"&numbers=[{number}]";
            string enableSMSParameter = $"&enable_sms=yes";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{numbersParameter}{enableSMSParameter}";

            try
            {
                return await route.GetJsonAsync<DidsOffnet>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Fatal($"{await ex.GetResponseStringAsync()}");
                return new DidsOffnet { code = 500, status = "error", error = await ex.GetResponseJsonAsync() };
            }
        }

        public static async Task<StatusResponse> StatusSubmitNumberAsync(string jobid, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "dids/offnet/submit";
            string tokenParameter = $"?token={token}";
            string jobidParameter = $"&jobid={jobid}";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{jobidParameter}";

            try
            {
                return await route.GetJsonAsync<StatusResponse>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Fatal($"{await ex.GetResponseStringAsync()}");
                return new StatusResponse { code = 500, status = "Error", data = await ex.GetResponseJsonAsync() };
            }
        }
    }
}
