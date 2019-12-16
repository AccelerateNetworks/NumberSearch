using Microsoft.Extensions.Configuration;
using NumberSearch.DataAccess;
using System;
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
            var postgresSQL = config.GetConnectionString("Postgresql");
            var bulkVSKey = config.GetConnectionString("BulkVSAPIKEY");
            var bulkVSSecret = config.GetConnectionString("BulkVSAPISecret");
            var username = config.GetConnectionString("PComNetUsername");
            var password = config.GetConnectionString("PComNetPassword");

            var start = DateTime.Now;

            //var teleStats = await TeleMessage.IngestPhoneNumbersAsync(teleToken, postgresSQL);
            var teleStats = new IngestStatistics { };

            //if (await teleStats.PostAsync(postgresSQL))
            //{
            //    Console.WriteLine("Ingest logged to the database.");
            //}
            //else
            //{
            //    Console.WriteLine("Failed to log this ingest.");
            //}

            var BulkVSStats = await BulkVS.IngestPhoneNumbersAsync(bulkVSKey, bulkVSSecret, postgresSQL);
            //var BulkVSStats = new IngestStatistics { };
            //var teleStats = await FirstCom.IngestPhoneNumbersAsync(username, password, postgresSQL);

            // TODO: Remove numbers from the database that weren't reingested.

            if (await BulkVSStats.PostAsync(postgresSQL))
            {
                Console.WriteLine("Ingest logged to the database.");
            }
            else
            {
                Console.WriteLine("Failed to log this ingest.");
            }

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
                NumbersRetrived = teleStats.NumbersRetrived + BulkVSStats.NumbersRetrived,
                FailedToIngest = teleStats.FailedToIngest + BulkVSStats.FailedToIngest,
                IngestedNew = teleStats.IngestedNew + BulkVSStats.IngestedNew,
                UpdatedExisting = teleStats.UpdatedExisting + BulkVSStats.UpdatedExisting,
                Unchanged = teleStats.Unchanged + BulkVSStats.Unchanged,
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
    }
}
