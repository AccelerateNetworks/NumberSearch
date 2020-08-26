using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BulkVS
{
    public class MainBulkVS
    {
        /// <summary>
        /// Scrape the bulkVS API for all of the valid phones that begin with a specific area code.
        /// </summary>
        /// <param name="apiKey"> The bulkVS API key. </param>
        /// <param name="apiSecret"> The bulkVS secret. </param>
        /// <returns></returns>
        public static async Task<PhoneNumber[]> GetValidNumbersByNPAAsync(string apiKey, string apiSecret, int[] areaCodes)
        {
            var numbers = new List<PhoneNumber>();

            foreach (var code in areaCodes)
            {
                try
                {
                    numbers.AddRange(await NpaBulkVS.GetAsync(code.ToString(), apiKey, apiSecret).ConfigureAwait(false));
                    Log.Information($"[BulkVS] Found {numbers.Count} Phone Numbers");
                }
                catch (Exception ex)
                {
                    Log.Error($"[BulkVS] Area code {code} failed @ {DateTime.Now}: {ex.Message}");
                }
            }

            return numbers.ToArray();
        }
    }
}