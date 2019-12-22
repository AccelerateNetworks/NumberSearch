using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class FirstCom
    {
        public static async Task<IngestStatistics> IngestPhoneNumbersAsync(string username, string password, string connectionString)
        {     
            var stats = await Program.SubmitPhoneNumbersAsync(await GetValidNumbersByNPAAsync(username, password), connectionString);

            return stats;
        }

        public static async Task<PhoneNumber[]> GetValidNumbersByNPAAsync(string username, string password)
        {
            var areaCodes = AreaCode.AreaCodes;

            var numbers = new List<PhoneNumber>();

            foreach(var code in areaCodes)
            {
                numbers.AddRange(await NpaNxxFirstPointCom.GetAsync(code.ToString(), string.Empty, string.Empty, username, password));
                Console.WriteLine($"Found {numbers.Count} Phone Numbers");
            }

            return numbers.ToArray();
        }
    }
}
