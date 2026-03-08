using Flurl.Http;

using Serilog;

namespace NumberSearch.DataAccess.FCC
{
    public readonly record struct ListAsOfDatesDatum(string data_type, string as_of_date);

    public readonly record struct ListAsOfDates(ListAsOfDatesDatum[] data, int result_count, int status_code, string message, string status, string request_date)
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static async Task<ListAsOfDates> GetAsync(ReadOnlyMemory<char> username, ReadOnlyMemory<char> apiKey)
        {
            string baseUrl = "https://broadbandmap.fcc.gov/api/public/";
            string endpoint = $"map/listAsOfDates";
            string route = $"{baseUrl}{endpoint}";

            try
            {
                var result = await route.WithHeader("username", username.ToString()).WithHeader("hash_value", apiKey.ToString()).GetJsonAsync<ListAsOfDates>();
                return result;
            }
            catch (FlurlHttpException ex)
            {
                Log.Error(await ex.GetResponseStringAsync());
                return new();
            }
        }
    }


    public readonly record struct ListAvailabilityData(ListAvailabilityDataDatum[] data, int result_count, int status_code, string message, string status, string request_date)
    {
        public static async Task<ListAvailabilityData> GetAsync(ReadOnlyMemory<char> date, ReadOnlyMemory<char> username, ReadOnlyMemory<char> apiKey)
        {
            string baseUrl = "https://broadbandmap.fcc.gov/api/public/";
            string endpoint = $"map/downloads/listAvailabilityData/{date}";
            string query = $"?category=State&technology_type=Fixed Broadband";
            string route = $"{baseUrl}{endpoint}{query}";

            try
            {
                var result = await route.WithHeader("username", username.ToString()).WithHeader("hash_value", apiKey.ToString()).GetJsonAsync<ListAvailabilityData>();
                return result;
            }
            catch (FlurlHttpException ex)
            {
                Log.Error(await ex.GetResponseStringAsync());
                return new();
            }
        }
    }

    public readonly record struct ListAvailabilityDataDatum(int file_id, string category, string subcategory, string technology_type, string technology_code, string technology_code_desc, string speed_tier, string state_fips, string state_name, string provider_id, string provider_name, string file_type, string file_name, string record_count)
    {
        public async Task<string> DownloadFileAsync(string folderPath, ReadOnlyMemory<char> username, ReadOnlyMemory<char> apiKey)
        {
            string baseUrl = "https://broadbandmap.fcc.gov/api/public/";
            string endpoint = $"map/downloads/downloadFile/availability/";
            string route = $"{baseUrl}{endpoint}{file_id}";

            try
            {
                var result = await route.WithHeader("username", username.ToString()).WithHeader("hash_value", apiKey.ToString()).DownloadFileAsync(folderPath);
                return result;
            }
            catch (FlurlHttpException ex)
            {
                Log.Error(await ex.GetResponseStringAsync());
                return await ex.GetResponseStringAsync();
            }
        }
    };

}
