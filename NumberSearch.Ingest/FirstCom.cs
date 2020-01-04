using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class FirstCom
    {
        /// <summary>
        /// Ingest phone numbers from the FirstCom API.
        /// </summary>
        /// <param name="username"> The firstCom username. </param>
        /// <param name="password"> The firstCom password. </param>
        /// <param name="connectionString"> the connection string for the database. </param>
        /// <returns></returns>
        public static async Task<IngestStatistics> IngestPhoneNumbersAsync(string username, string password, string connectionString)
        {     
            var stats = await Program.SubmitPhoneNumbersAsync(await GetValidNumbersByNPAAsync(username, password), connectionString);

            return stats;
        }

        /// <summary>
        /// Gets a list of valid phone numbers that begin with an area code.
        /// </summary>
        /// <param name="username"> The firstcom username. </param>
        /// <param name="password"> The firstCom password. </param>
        /// <returns></returns>
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
