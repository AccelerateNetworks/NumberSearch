using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("40f816f3-0a65-4523-a9be-4bbef0716720")
            .Build();

            var teleToken = Guid.Parse(config.GetConnectionString("TeleAPI"));
            var postgresSQL = config.GetConnectionString("PostgresqlProd");
            var bulkVSKey = config.GetConnectionString("BulkVSAPIKEY");
            var bulkVSSecret = config.GetConnectionString("BulkVSAPISecret");
            var username = config.GetConnectionString("PComNetUsername");
            var password = config.GetConnectionString("PComNetPassword");

            var start = DateTime.Now;

            // Ingest all avalible numbers from the TeleAPI.
            //var teleStats = await TeleMessage.IngestPhoneNumbersAsync(teleToken, postgresSQL);
            var teleStats = new IngestStatistics { };

            if (await teleStats.PostAsync(postgresSQL))
            {
                Console.WriteLine("Ingest logged to the database.");
            }
            else
            {
                Console.WriteLine("Failed to log this ingest.");
            }

            // Ingest all avablie phones numbers from the BulkVs API.
            var BulkVSStats = await BulkVS.IngestPhoneNumbersAsync(bulkVSKey, bulkVSSecret, postgresSQL);
            //var BulkVSStats = new IngestStatistics { };

            if (await BulkVSStats.PostAsync(postgresSQL))
            {
                Console.WriteLine("Ingest logged to the database.");
            }
            else
            {
                Console.WriteLine("Failed to log this ingest.");
            }

            // Ingest all avalible numbers in the FirstCom API.
            //var FirstComStats = await FirstCom.IngestPhoneNumbersAsync(username, password, postgresSQL);
            var FirstComStats = new IngestStatistics { };

            if (await FirstComStats.PostAsync(postgresSQL))
            {
                Console.WriteLine("Ingest logged to the database.");
            }
            else
            {
                Console.WriteLine("Failed to log this ingest.");
            }

            // Remove all of the old numbers from the database.
            var cleanUp = await PhoneNumber.DeleteOld(start, postgresSQL);

            if (await cleanUp.PostAsync(postgresSQL))
            {
                Console.WriteLine("Old numbers removed from the database.");
            }
            else
            {
                Console.WriteLine("Failed to remove old numbers from the database.");
            }

            var end = DateTime.Now;

            var combinedStats = new IngestStatistics
            {
                NumbersRetrived = teleStats.NumbersRetrived + BulkVSStats.NumbersRetrived + FirstComStats.NumbersRetrived,
                FailedToIngest = teleStats.FailedToIngest + BulkVSStats.FailedToIngest + FirstComStats.FailedToIngest,
                IngestedNew = teleStats.IngestedNew + BulkVSStats.IngestedNew + FirstComStats.IngestedNew,
                UpdatedExisting = teleStats.UpdatedExisting + BulkVSStats.UpdatedExisting + FirstComStats.UpdatedExisting,
                Unchanged = teleStats.Unchanged + BulkVSStats.Unchanged + FirstComStats.Unchanged,
                Removed = cleanUp.Removed,
                IngestedFrom = "All",
                StartDate = start,
                EndDate = end
            };

            var check = combinedStats.PostAsync(postgresSQL);

            var diff = end - start;

            Console.WriteLine($"Numbers Retrived: {combinedStats.NumbersRetrived}");
            Console.WriteLine($"Numbers Ingested New: {combinedStats.IngestedNew}");
            Console.WriteLine($"Numbers Updated Existing: {combinedStats.UpdatedExisting}");
            Console.WriteLine($"Numbers Unchanged: {combinedStats.Unchanged}");
            Console.WriteLine($"Numbers Removed: {combinedStats.Removed}");
            Console.WriteLine($"Numbers Failed To Ingest: {combinedStats.FailedToIngest}");
            Console.WriteLine($"Start: {start.ToLongTimeString()} End: {end.ToLongTimeString()} Elapsed: {diff.TotalMinutes} Minutes");
        }

        /// <summary>
        /// Split the list of accounts in insert into smaller lists so that they can be submitted in bulk to the database in reasonably sized chunks.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="locations"></param>
        /// <param name="nSize"> The maximum number of items in the smaller lists. </param>
        /// <returns></returns>
        public static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize = 100)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }

        /// <summary>
        /// Submit the ingested Phone numbers to the database in bulk to minimize the number of commands that have to be sent.
        /// </summary>
        /// <param name="numbers"> A list of phone numbers. </param>
        /// <param name="connectionString"> The connection string for the database. </param>
        /// <returns></returns>
        public static async Task<IngestStatistics> SubmitPhoneNumbersAsync(PhoneNumber[] numbers, string connectionString)
        {
            var stats = new IngestStatistics();

            var inserts = new List<PhoneNumber>();

            if (numbers.Length > 0)
            {
                var existingNumbers = await PhoneNumber.GetAllAsync(connectionString);
                var dict = existingNumbers.ToDictionary(x => x.DialedNumber, x => x);
                // Submit the batch to the remote database.
                foreach (var number in numbers)
                {
                    // Check if it already exists.
                    var inDb = number.ExistsInDb(dict);

                    if (inDb)
                    {
                        var existingNumber = await PhoneNumber.GetAsync(number.DialedNumber, connectionString);

                        if (!(existingNumber.IngestedFrom == number.IngestedFrom))
                        {
                            var status = await number.PutAsync(connectionString);

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
                        inserts.Add(number);

                        stats.NumbersRetrived++;
                        stats.IngestedNew++;
                    }
                }
            }

            var groups = SplitList(inserts);

            foreach (var group in groups?.ToArray())
            {
                var check = await PhoneNumber.BulkPostAsync(group, connectionString);

                if (check) { stats.IngestedNew += 100; };

                Console.WriteLine($"{stats.IngestedNew} of {numbers.Length} submitted to the database.");
            }

            return stats;
        }

    }
}