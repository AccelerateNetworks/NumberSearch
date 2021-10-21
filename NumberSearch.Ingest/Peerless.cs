using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Peerless;

using Serilog;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class Peerless
    {
        /// <summary>
        /// Gets a list of valid phone numbers that begin with an area code.
        /// </summary>
        /// <param name="username"> The firstcom username. </param>
        /// <param name="password"> The firstCom password. </param>
        /// <returns></returns>
        public static async Task<PhoneNumber[]> GetValidNumbersByNPAAsync(int[] areaCodes, string apiKey)
        {
            var numbers = new List<PhoneNumber>();

            foreach (var code in areaCodes)
            {
                try
                {
                    numbers.AddRange(await DidFind.GetByNPAAsync(code.ToString(), apiKey).ConfigureAwait(false));
                    Log.Information($"[Peerless] Found {numbers.Count} Phone Numbers for NPA {code}");
                }
                catch (Exception ex)
                {
                    Log.Error($"[Peerless] Area code {code} failed @ {DateTime.Now}: {ex.Message}");
                }
            }

            return numbers.ToArray();
        }
    }
}