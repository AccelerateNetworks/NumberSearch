using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class IngestStatistics
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int NumbersRetrived { get; set; }
        public int IngestedNew { get; set; }
        public int FailedToIngest { get; set; }
        public int UpdatedExisting { get; set; }
        public int Unchanged { get; set; }
        public int Removed { get; set; }
        public string IngestedFrom { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now;
        public bool Lock { get; set; } = false;
        public bool Priority { get; set; } = false;

        public enum IngestSource
        {
            DatabaseCleanup,
            TeleMessage,
            FirstPointCom,
            BulkVS,
            All
        }

        public static async Task<IEnumerable<IngestStatistics>> GetAllAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<IngestStatistics>("SELECT \"Id\", \"NumbersRetrived\", \"IngestedNew\", \"FailedToIngest\", \"UpdatedExisting\", \"Unchanged\", \"Removed\", \"IngestedFrom\", \"StartDate\", \"EndDate\", \"Lock\", \"Priority\" FROM public.\"Ingests\" ORDER BY \"StartDate\" DESC LIMIT 200")
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IngestStatistics> GetLastIngestAsync(string ingestedFrom, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<IngestStatistics>("SELECT \"Id\", \"NumbersRetrived\", \"IngestedNew\", \"FailedToIngest\", \"UpdatedExisting\", \"Unchanged\", \"Removed\", \"IngestedFrom\", \"StartDate\", \"EndDate\", \"Lock\", \"Priority\" FROM public.\"Ingests\" WHERE \"IngestedFrom\" = @ingestedFrom AND \"Priority\" = false ORDER BY \"StartDate\" DESC LIMIT 1",
                new { ingestedFrom })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IngestStatistics> GetLockAsync(string ingestedFrom, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<IngestStatistics>("SELECT \"Id\", \"NumbersRetrived\", \"IngestedNew\", \"FailedToIngest\", \"UpdatedExisting\", \"Unchanged\", \"Removed\", \"IngestedFrom\", \"StartDate\", \"EndDate\", \"Lock\", \"Priority\" FROM public.\"Ingests\" WHERE \"IngestedFrom\" = @ingestedFrom AND \"Lock\" = true ORDER BY \"StartDate\" DESC LIMIT 1",
                new { ingestedFrom })
                .ConfigureAwait(false);

            return result;
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"Ingests\"(\"Id\", \"NumbersRetrived\", \"IngestedNew\", \"FailedToIngest\", \"UpdatedExisting\", \"Unchanged\", \"Removed\", \"IngestedFrom\", \"StartDate\", \"EndDate\", \"Lock\", \"Priority\") " +
                "VALUES (@Id, @NumbersRetrived, @IngestedNew, @FailedToIngest, @UpdatedExisting, @Unchanged, @Removed, @IngestedFrom, @StartDate, @EndDate, @Lock, @Priority)", new { Id, NumbersRetrived, IngestedNew, FailedToIngest, UpdatedExisting, Unchanged, Removed, IngestedFrom, StartDate, EndDate, Lock, Priority })
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

        public async Task<bool> DeleteAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"Ingests\" WHERE \"Id\" = @Id", new { Id })
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
