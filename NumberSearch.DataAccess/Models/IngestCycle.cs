using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class IngestCycle
    {
        public Guid IngestCycleId { get; set; }
        public string IngestedFrom { get; set; }
        public TimeSpan CycleTime { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool Enabled { get; set; }


        public static async Task<IEnumerable<IngestCycle>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<IngestCycle>("SELECT \"IngestCycleId\", \"IngestedFrom\", \"CycleTime\", \"LastUpdate\", \"Enabled\" FROM public.\"IngestCycles\"")
                .ConfigureAwait(false);

            return result;
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"IngestCycles\" (\"IngestedFrom\", \"CycleTime\", \"LastUpdate\", \"Enabled\") VALUES( @IngestedFrom, @CycleTime, @LastUpdate, @Enabled)",
                new { IngestedFrom, CycleTime, LastUpdate, Enabled })
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

        public async Task<bool> PutAsync(string connectionString)
        {
            if (IngestCycleId == Guid.Empty)
            {
                return false;
            }

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"IngestCycles\" SET \"IngestedFrom\" = @IngestedFrom, \"CycleTime\" = @CycleTime, \"LastUpdate\" = @LastUpdate, \"Enabled\" = @Enabled WHERE \"IngestCycleId\" = @IngestCycleId",
                new { IngestedFrom, CycleTime, LastUpdate, Enabled, IngestCycleId })
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
            if (IngestCycleId == Guid.Empty)
            {
                return false;
            }

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"IngestCycles\" WHERE \"IngestCycleId\" = @IngestCycleId", new { IngestCycleId })
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
