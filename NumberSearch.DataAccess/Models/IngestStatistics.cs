using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class IngestStatistics
    {
        public int NumbersRetrived { get; set; }
        public int IngestedNew { get; set; }
        public int FailedToIngest { get; set; }
        public int UpdatedExisting { get; set; }
        public int Unchanged { get; set; }
        public int Removed { get; set; }
        public string IngestedFrom { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public enum IngestSource
        {
            DatabaseCleanup,
            TeleMessage,
            FirstCom,
            BulkVS,
            All
        }

        public static async Task<IEnumerable<IngestStatistics>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<IngestStatistics>("SELECT \"Id\", \"NumbersRetrived\", \"IngestedNew\", \"FailedToIngest\", \"UpdatedExisting\", \"Unchanged\", \"Removed\", \"IngestedFrom\", \"StartDate\", \"EndDate\" FROM public.\"Ingests\" ORDER BY \"EndDate\" DESC")
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IngestStatistics> GetLastIngestAsync(string ingestedFrom, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<IngestStatistics>("SELECT \"Id\", \"NumbersRetrived\", \"IngestedNew\", \"FailedToIngest\", \"UpdatedExisting\", \"Unchanged\", \"Removed\", \"IngestedFrom\", \"StartDate\", \"EndDate\" FROM public.\"Ingests\" WHERE \"IngestedFrom\" = @ingestedFrom ORDER BY \"StartDate\" DESC LIMIT 1",
                new { ingestedFrom })
                .ConfigureAwait(false);

            return result;
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"Ingests\"( \"NumbersRetrived\", \"IngestedNew\", \"FailedToIngest\", \"UpdatedExisting\", \"Unchanged\", \"Removed\", \"IngestedFrom\", \"StartDate\", \"EndDate\") " +
                "VALUES (@NumbersRetrived, @IngestedNew, @FailedToIngest, @UpdatedExisting, @Unchanged, @Removed, @IngestedFrom, @StartDate, @EndDate)", new { NumbersRetrived, IngestedNew, FailedToIngest, UpdatedExisting, Unchanged, Removed, IngestedFrom, StartDate, EndDate })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
