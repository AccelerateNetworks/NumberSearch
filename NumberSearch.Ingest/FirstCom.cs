using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class FirstCom
    {
        public static async Task<IngestStatistics> IngestPhoneNumbersAsync(string username, string password, string connectionString)
        {     
            var stats = await SubmitPhoneNumbersAsync(await GetValidNumbersByNPAAsync(username, password), connectionString);

            return stats;
        }

        public static async Task<PhoneNumber[]> GetValidNumbersByNPAAsync(string username, string password)
        {
            var areaCodes = AreaCode.AreaCodes;

            var numbers = new List<PhoneNumber>();

            foreach(var code in areaCodes.Where(x => x == 206).ToArray())
            {
                numbers.AddRange(await NpaNxxFirstPointCom.GetAsync(code.ToString(), string.Empty, string.Empty, username, password));
                Console.WriteLine($"Found {numbers.Count} Phone Numbers");
            }

            return numbers.ToArray();
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
