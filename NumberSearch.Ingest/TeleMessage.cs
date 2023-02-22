using NumberSearch.DataAccess;
using NumberSearch.DataAccess.TeliMessage;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class TeliMessage
    {
        /// <summary>
        /// Gets a list of valid area codes.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<int[]> GetValidNPAsAsync(Guid token)
        {
            var results = await DidsNpas.GetAsync(token).ConfigureAwait(false);

            if (results?.status is not "Success" && results?.code is not 200)
            {
                return Array.Empty<int>();
            }

            var valid = new List<int>();

            if (results?.data is not null)
            {
                foreach (var npa in results.data.ToArray())
                {
                    // Valid NPAs are only 3 chars long.
                    if (npa.Length is 3)
                    {
                        var check = int.TryParse(npa, out int outNpa);

                        if (check && PhoneNumbersNA.AreaCode.ValidNPA(outNpa))
                        {
                            valid.Add(outNpa);
                        }
                    }
                }
            }

            return valid.ToArray();
        }

        /// <summary>
        /// Gets a list of valid XXXX's for a given NPA.
        /// </summary>
        /// <param name="npa"> The area code. </param>
        /// <param name="nxx"> The NXX. </param>
        /// <param name="token"> The TeleMessage auth token. </param>
        /// <returns></returns>
        public static async Task<PhoneNumber[]> GetXXXXsByNPAAsync(int npa, Guid token)
        {
            var vaild = new List<PhoneNumber>();

            try
            {
                var results = await DidsList.GetAsync(npa, token).ConfigureAwait(false);

                if (results is not null)
                {
                    foreach (var result in results.ToArray())
                    {
                        if (result.XXXX > 1)
                        {
                            vaild.Add(result);
                        }
                    }
                }
                else
                {
                    Log.Information($"No results for NPA code {npa}.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"NPA code {npa} failed @ {DateTime.Now}: {ex.Message}");
            }

            return vaild.ToArray();
        }
    }
}
