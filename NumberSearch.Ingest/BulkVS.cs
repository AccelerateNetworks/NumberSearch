using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class BulkVS
    {
        public static async Task<IngestStatistics> IngestPhoneNumbersAsync(string apiKey, string apiSecret, string connectionString)
        {
            var start = DateTime.Now;

            var stats = await Program.SubmitPhoneNumbersAsync(await GetValidNumbersByNPAAsync(apiKey, apiSecret), connectionString);

            var end = DateTime.Now;
            stats.StartDate = start;
            stats.EndDate = end;
            stats.IngestedFrom = "BulkVS";

            return stats;
        }

        public static async Task<PhoneNumber[]> GetValidNumbersByNPAAsync(string apiKey, string apiSecret)
        {
            var areaCodes = AreaCode.AreaCodes;

            var numbers = new List<PhoneNumber>();

            foreach(var code in areaCodes.Take(10).ToArray())
            {
                numbers.AddRange(await NpaBulkVS.GetAsync(code.ToString(), apiKey, apiSecret));
                Console.WriteLine($"Found {numbers.Count} Phone Numbers");
            }

            return numbers.ToArray();
        }
    }
}
