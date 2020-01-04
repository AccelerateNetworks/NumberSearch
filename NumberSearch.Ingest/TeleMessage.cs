using NumberSearch.DataAccess;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class TeleMessage
    {
        /// <summary>
        /// Ingests phone number from the TeleMessage API.
        /// </summary>
        /// <param name="token"> The teleMesssage token. </param>
        /// <param name="connectionString"> The connection string for the database. </param>
        /// <returns></returns>
        public static async Task<IngestStatistics> IngestPhoneNumbersAsync(Guid token, string connectionString)
        {
            var readyToSubmit = new List<PhoneNumber>();

            var start = DateTime.Now;

            var npas = await GetValidNPAsAsync(token);

            Console.WriteLine($"Found {npas.Length} NPAs");

            foreach (var npa in npas)
            {
                var nxxs = await GetValidNXXsAsync(npa, token);

                Console.WriteLine($"Found {nxxs.Length} NXXs");
                foreach (var nxx in nxxs)
                {
                    readyToSubmit.AddRange(await GetValidXXXXsAsync(npa, nxx, token));
                    Console.WriteLine($"Found {readyToSubmit.Count} Phone Numbers");
                }
            }

            var stats = await Program.SubmitPhoneNumbersAsync(readyToSubmit.ToArray(), connectionString);

            var end = DateTime.Now;
            stats.StartDate = start;
            stats.EndDate = end;
            stats.IngestedFrom = "TeleMessage";

            return stats;
        }

        /// <summary>
        /// Gets a list of valid area codes.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<int[]> GetValidNPAsAsync(Guid token)
        {
            var results = await TeleNPA.GetAsync(token);

            if (!(results.status == "Success") && !(results.code == 200))
            {
                return new int[] { };
            }

            var valid = new List<int>();
            foreach (var npa in results?.data?.ToArray())
            {
                // Valid NPAs are only 3 chars long.
                if (npa.Length == 3)
                {
                    var check = int.TryParse(npa, out int outNpa);

                    if (check && outNpa > 99)
                    {
                        valid.Add(outNpa);
                    }
                }
            }

            return valid.ToArray();
        }

        /// <summary>
        /// gets a list of valid NXX's from a given area code.
        /// </summary>
        /// <param name="npa"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<int[]> GetValidNXXsAsync(int npa, Guid token)
        {
            var results = await TeleNXX.GetAsync($"{npa}", token);

            var vaild = new List<int>();

            // Verify that we got a good response.
            if ((results.status == "success") && (results.code == 200))
            {
                foreach (var result in results?.data?.ToArray())
                {
                    // Valid NXXs are only 3 chars long.
                    if (result.Length == 3)
                    {
                        bool check = int.TryParse(result, out int nxx);

                        if (check && nxx > 99)
                        {
                            vaild.Add(nxx);
                        }
                    }
                }
            }

            return vaild.ToArray();
        }

        /// <summary>
        /// Gets a list of valid XXXX's for a given NXX.
        /// </summary>
        /// <param name="npa"> The area code. </param>
        /// <param name="nxx"> The NXX. </param>
        /// <param name="token"> The TeleMessage auth token. </param>
        /// <returns></returns>
        public static async Task<PhoneNumber[]> GetValidXXXXsAsync(int npa, int nxx, Guid token)
        {
            var vaild = new List<PhoneNumber>();

            var results = await LocalNumberTeleMessage.GetAsync($"{npa}{nxx}****", token);

            foreach (var result in results?.ToArray())
            {
                if (result.XXXX > 1)
                {
                    vaild.Add(result);
                }
            }

            return vaild.ToArray();
        }
    }
}
