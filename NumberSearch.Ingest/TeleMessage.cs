using NumberSearch.DataAccess;
using NumberSearch.DataAccess.TeleMesssage;

using Serilog;

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
            var numbersReady = new List<PhoneNumber>();

            var start = DateTime.Now;

            var npas = new int[] { };

            try
            {
                npas = await GetValidNPAsAsync(token);

                Log.Information($"[TeleMessage] Found {npas.Length} NPAs");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}");
                Log.Error($"[TeleMessage] No NPAs Retrived.");
            }

            foreach (var npa in npas)
            {
                var nxxs = new int[] { };

                try
                {
                    nxxs = await GetValidNXXsAsync(npa, token);

                    Log.Information($"[TeleMessage] Found {nxxs.Length} NXXs for NPA {npa}");
                }
                catch (Exception ex)
                {
                    Log.Error($"{ex.Message}");
                    Log.Error($"[TeleMessage] No NXXs Retrived for NPA {npa}.");
                }

                if (nxxs.Length > 1)
                {
                    foreach (var nxx in nxxs)
                    {
                        var localNumbers = await GetValidXXXXsAsync(npa, nxx, token);
                        foreach (var number in localNumbers)
                        {
                            numbersReady.Add(number);
                        }
                        Log.Information($"[TeleMessage] Found {localNumbers.Length} Phone Numbers for {npa}-{nxx}-xxxx");
                    }
                }
            }

            var typedNumbers = Program.AssignNumberTypes(numbersReady).ToArray();

            var stats = await Program.SubmitPhoneNumbersAsync(typedNumbers, connectionString);

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
            var results = await DidsNpas.GetAsync(token);

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
            var results = await DidsNxxs.GetAsync($"{npa}", token);

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

            try
            {
                var results = await DidsList.GetAsync($"{npa}{nxx}****", token);

                foreach (var result in results?.ToArray())
                {
                    if (result.XXXX > 1)
                    {
                        vaild.Add(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"NXX code {nxx} failed @ {DateTime.Now}: {ex.Message}");
            }

            return vaild.ToArray();
        }
    }
}
