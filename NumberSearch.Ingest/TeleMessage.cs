using NumberSearch.DataAccess;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class TeleMessage
    {
        public static async Task<IngestStatistics> IngestPhoneNumbersAsync(Guid token, string connectionString)
        {
            var readyToSubmit = new List<PhoneNumber>();

            var start = DateTime.Now;

            var npas = await GetValidNPAsAsync(token);

            Console.WriteLine($"Found {npas.Length} NPAs");

            foreach (var npa in npas.ToArray())
            {
                var nxxs = await GetValidNXXsAsync(npa, token);

                Console.WriteLine($"Found {nxxs.Length} NXXs");
                foreach (var nxx in nxxs)
                {
                    readyToSubmit.AddRange(await GetValidXXXXsAsync(npa, nxx, token));
                    Console.WriteLine($"Found {readyToSubmit.Count} Phone Numbers");
                }
            }

            var stats = await SubmitPhoneNumbersAsync(readyToSubmit.ToArray(), connectionString);

            var end = DateTime.Now;
            stats.StartDate = start;
            stats.EndDate = end;
            stats.IngestedFrom = "TeleMessage";

            return stats;
        }

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

        public static async Task<int[]> GetValidNXXsAsync(int npa, Guid token)
        {
            var results = await TeleNXX.GetAsync($"{npa}", token);

            var vaild = new List<int>();

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

        public static async Task<IngestStatistics> SubmitPhoneNumbersAsync(PhoneNumber[] numbers, string connnectionString)
        {
            var stats = new IngestStatistics();

            if (numbers.Length > 0)
            {
                // Submit the batch to the remote database.
                foreach (var number in numbers)
                {
                    // Check if it already exists.
                    var inDb = await number.ExistsInDb(connnectionString);

                    if (inDb)
                    {
                        var existingNumber = await PhoneNumber.GetAsync(number.DialedNumber, connnectionString);

                        if (!(existingNumber.IngestedFrom == number.IngestedFrom))
                        {
                            var status = await number.PutAsync(connnectionString);

                            if (status)
                            {
                                stats.NumbersRetrived++;
                                stats.UpdatedExisting++;
                            }
                            else
                            {
                                stats.NumbersRetrived++;
                                stats.FailedToIngest++;
                            }
                        }
                        else
                        {
                            stats.NumbersRetrived++;
                            stats.Unchanged++;
                        }
                    }
                    else
                    {
                        // If it doesn't exist then add it.
                        var status = await number.PostAsync(connnectionString);

                        if (status)
                        {
                            stats.NumbersRetrived++;
                            stats.IngestedNew++;
                        }
                        else
                        {
                            stats.NumbersRetrived++;
                            stats.FailedToIngest++;
                        }
                    }
                }
            }

            return stats;
        }
    }
}
