using NumberSearch.DataAccess.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirstCom
{
    public class FirstPointCom
    {
        /// <summary>
        /// Gets a list of valid phone numbers that begin with an area code.
        /// </summary>
        /// <param name="username"> The FirstPointCom username. </param>
        /// <param name="password"> The FirstPointCom password. </param>
        /// <returns></returns>
        public static async Task<PhoneNumber[]> GetValidNumbersByNPAAsync(ReadOnlyMemory<char> username, ReadOnlyMemory<char> password, int[] areaCodes)
        {
            var numbers = new List<PhoneNumber>();

            foreach (var code in areaCodes)
            {
                try
                {
                    numbers.AddRange(await NpaNxxFirstPointCom.GetAsync(code.ToString().AsMemory(), string.Empty.AsMemory(), string.Empty.AsMemory(), username, password));
                    Log.Information($"[FirstPointCom] Found {numbers.Count} Phone Numbers");
                }
                catch (Exception ex)
                {
                    Log.Error($"Area code {code} failed @ {DateTime.Now}: {ex.Message}");
                }
            }

            return [.. numbers];
        }
    }
}
