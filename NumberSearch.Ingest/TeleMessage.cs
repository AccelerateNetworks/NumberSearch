using NumberSearch.DataAccess;
using NumberSearch.DataAccess.TeliMesssage;

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

            if (!(results.status == "Success") && !(results.code == 200))
            {
                return Array.Empty<int>();
            }

            var valid = new List<int>();
            foreach (var npa in results.data.ToArray())
            {
                // Valid NPAs are only 3 chars long.
                if (npa.Length == 3)
                {
                    var check = int.TryParse(npa, out int outNpa);

                    if (check && PhoneNumbersNA.AreaCode.ValidNPA(outNpa))
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
            var results = await DidsNxxs.GetAsync($"{npa}", token).ConfigureAwait(false);

            var vaild = new List<int>();

            // Verify that we got a good response.
            if ((results.status == "success") && (results.code == 200))
            {
                foreach (var result in results.data.ToArray())
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
                var results = await DidsList.GetAsync($"{npa}{nxx}****", token).ConfigureAwait(false);

                foreach (var result in results.ToArray())
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

                foreach (var result in results.ToArray())
                {
                    if (result.XXXX > 1)
                    {
                        vaild.Add(result);
                    }
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
