using Microsoft.Extensions.Configuration;

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

            var start = DateTime.Now;

            //var teleStats = await TeleMessage.IngestPhoneNumbersAsync(teleToken, postgresSQL);
            var teleStats = await BulkVS.IngestPhoneNumbersAsync(bulkVSKey, bulkVSSecret, postgresSQL);

            var end = DateTime.Now;
            var diff = end - start;

            Console.WriteLine($"Numbers Retrived: {teleStats.NumbersRetrived}");
            Console.WriteLine($"Numbers Ingested New: {teleStats.IngestedNew}");
            Console.WriteLine($"Numbers Updated Existing: {teleStats.UpdatedExisting}");
            Console.WriteLine($"Numbers Unchanged: {teleStats.Unchanged}");
            Console.WriteLine($"Numbers Failed To Ingest: {teleStats.FailedToIngest}");
            Console.WriteLine($"Start: {start.ToLongTimeString()} End: {end.ToLongTimeString()} Elapsed: {diff.TotalMinutes} Minutes");
        }
    }
}
