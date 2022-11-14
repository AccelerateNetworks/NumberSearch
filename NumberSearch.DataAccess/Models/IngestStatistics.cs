using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class IngestStatistics
    {
        public Guid? Id { get; set; }
        public int NumbersRetrived { get; set; }
        public int IngestedNew { get; set; }
        public int FailedToIngest { get; set; }
        public int UpdatedExisting { get; set; }
        public int Unchanged { get; set; }
        public int Removed { get; set; }
        public string IngestedFrom { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool Lock { get; set; }
        public bool Priority { get; set; }

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
                .ExecuteAsync("INSERT INTO public.\"Ingests\"( \"NumbersRetrived\", \"IngestedNew\", \"FailedToIngest\", \"UpdatedExisting\", \"Unchanged\", \"Removed\", \"IngestedFrom\", \"StartDate\", \"EndDate\", \"Lock\", \"Priority\") " +
                "VALUES (@NumbersRetrived, @IngestedNew, @FailedToIngest, @UpdatedExisting, @Unchanged, @Removed, @IngestedFrom, @StartDate, @EndDate, @Lock, @Priority)", new { NumbersRetrived, IngestedNew, FailedToIngest, UpdatedExisting, Unchanged, Removed, IngestedFrom, StartDate, EndDate, Lock, Priority })
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

        /// <summary>
        /// Update a phone number that already exists in the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PutAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"Ingests\" SET \"Id\" = @Id, \"NumbersRetrived\" = @NumbersRetrived, \"IngestedNew\" = @IngestedNew, \"FailedToIngest\" = @FailedToIngest, \"UpdatedExisting\" = @UpdatedExisting, \"Unchanged\" = @Unchanged, \"Removed\" = @Removed, \"IngestedFrom\" = @IngestedFrom, \"StartDate\" = @StartDate, \"EndDate\" = @EndDate, \"Lock\" = @Lock, \"Priority\" = @Priority WHERE \"Id\" = @Id",
                new { Id, NumbersRetrived, IngestedNew, FailedToIngest, UpdatedExisting, Unchanged, Removed, IngestedFrom, StartDate, EndDate, Lock, Priority })
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

            if (!Id.HasValue)
            {
                return false;
            }

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
