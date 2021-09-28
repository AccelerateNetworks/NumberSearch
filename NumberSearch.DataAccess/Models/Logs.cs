using Dapper;

using Npgsql;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Models
{
    public class Logs
    {
        /// <summary>
        /// Delete only numbers that haven't been reingested recently.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IngestStatistics> DeleteOld(DateTime ingestStart, string connectionString)
        {
            var start = DateTime.Now;

            using var connection = new NpgsqlConnection(connectionString);

            var ingest = await connection
                .ExecuteAsync("DELETE FROM \"Logs\".\"Ingest\" " +
                "WHERE raise_date < @DateIngested",
                new { DateIngested = ingestStart.AddDays(-3) })
                .ConfigureAwait(false);

             var mvc = await connection
                .ExecuteAsync("DELETE FROM \"Logs\".\"Mvc\" " +
                "WHERE raise_date < @DateIngested",
                new { DateIngested = ingestStart.AddDays(-3) })
                .ConfigureAwait(false);

            return new IngestStatistics
            {
                Removed = ingest + mvc,
                IngestedFrom = "Logs Cleanup",
                StartDate = start,
                EndDate = DateTime.Now
            };
        }
    }
}
