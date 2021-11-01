using Flurl.Http;

using NumberSearch.DataAccess.TeliMessage;

using Serilog;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeliMesssage
{
    public class DidsOffnet
    {
        public int code { get; set; }
        public string status { get; set; }
        public string error { get; set; }
        public OffnetResponseData data { get; set; }


        public class OffnetResponseData
        {
            public string[] success { get; set; }
            public string jobid { get; set; }
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
                var error = await ex.GetResponseJsonAsync<TeliError>();
                return new DidsOffnet { code = error.code, status = error.status, error = await ex.GetResponseJsonAsync() };
            }
        }

        public static async Task<DidsOffnet> SubmitNumberAsync(string number, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "dids/offnet/submit";
            string tokenParameter = $"?token={token}";
            string numbersParameter = $"&numbers={number}";
            string enableSMSParameter = $"&enable_sms=no";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{numbersParameter}{enableSMSParameter}";

            try
            {
                return await route.GetJsonAsync<DidsOffnet>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Fatal($"{await ex.GetResponseStringAsync()}");
                var error = await ex.GetResponseJsonAsync<TeliError>();
                return new DidsOffnet { code = error.code, status = error.status, error = await ex.GetResponseJsonAsync() };
            }
        }

        public static async Task<DidsOffnet> StatusSubmitNumberAsync(string jobid, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "dids/offnet/submit";
            string tokenParameter = $"?token={token}";
            string jobidParameter = $"&jobid={jobid}";
            string route = $"{baseUrl}{endpoint}{tokenParameter}{jobidParameter}";

            try
            {
                return await route.GetJsonAsync<DidsOffnet>().ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                Log.Fatal($"{await ex.GetResponseStringAsync()}");
                var error = await ex.GetResponseJsonAsync<TeliError>();
                return new DidsOffnet { code = error.code, status = error.status, error = await ex.GetResponseJsonAsync() };
            }
        }
    }
}
