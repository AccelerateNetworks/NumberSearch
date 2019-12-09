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
            // TODO: Ingest data into         
            var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("40f816f3-0a65-4523-a9be-4bbef0716720")
            .Build();

            var teleToken = Guid.Parse(config.GetConnectionString("TeleAPI"));
            var postgresSQL = config.GetConnectionString("Postgresql");

            var start = DateTime.Now;

            var teleStats = await IngestTeleMessagePhoneNumbers(teleToken, postgresSQL);

            var end = DateTime.Now;
            var diff = end - start;

            Console.WriteLine($"Numbers Retrived: {teleStats.NumbersRetrived}");
            Console.WriteLine($"Numbers Ingested New: {teleStats.IngestedNew}");
            Console.WriteLine($"Numbers Updated Existing: {teleStats.UpdatedExisting}");
            Console.WriteLine($"Numbers Unchanged: {teleStats.Unchanged}");
            Console.WriteLine($"Numbers Failed To Ingest: {teleStats.FailedToIngest}");
            Console.WriteLine($"Start: {start.ToLongTimeString()} End: {end.ToLongTimeString()} Elapsed: {diff.TotalMinutes} Minutes");
        }

        public static async Task<IngestStatistics> IngestTeleMessagePhoneNumbers(Guid token, string connectionString)
        {
            var readyToSubmit = new List<PhoneNumber>();

            var npas = await GetValidNPAs(token);

            Console.WriteLine($"Found {npas.Length} NPAs");

            foreach (var npa in npas.Where(x => x == 206).ToArray())
            {
                var nxxs = await GetValidNXXs(npa, token);

                Console.WriteLine($"Found {nxxs.Length} NXXs");
                foreach (var nxx in nxxs)
                {
                    readyToSubmit.AddRange(await GetValidXXXXs(npa, nxx, token));
                    Console.WriteLine($"Found {readyToSubmit.Count} Phone Numbers");
                }
            }

            var stats = await SubmitPhoneNumbers(readyToSubmit.ToArray(), connectionString);

            return stats;
        }

        public static async Task<int[]> GetValidNPAs(Guid token)
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

        public static async Task<int[]> GetValidNXXs(int npa, Guid token)
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

        public static async Task<PhoneNumber[]> GetValidXXXXs(int npa, int nxx, Guid token)
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

        public static async Task<IngestStatistics> SubmitPhoneNumbers(PhoneNumber[] numbers, string connnectionString)
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

        public class IngestStatistics
        {
            public int NumbersRetrived { get; set; }
            public int IngestedNew { get; set; }
            public int FailedToIngest { get; set; }
            public int UpdatedExisting { get; set; }
            public int Unchanged { get; set; }
        }
    }
}
