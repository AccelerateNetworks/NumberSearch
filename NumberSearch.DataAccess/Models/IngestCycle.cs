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
        public string IngestedFrom { get; set; } = string.Empty;
        public TimeSpan CycleTime { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool Enabled { get; set; }
        public bool RunNow { get; set; }


        public static async Task<IEnumerable<IngestCycle>> GetAllAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<IngestCycle>("SELECT \"IngestCycleId\", \"IngestedFrom\", \"CycleTime\", \"LastUpdate\", \"Enabled\", \"RunNow\" FROM public.\"IngestCycles\"")
                .ConfigureAwait(false);

            return result;
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"IngestCycles\" (\"IngestedFrom\", \"CycleTime\", \"LastUpdate\", \"Enabled\", \"RunNow\") VALUES( @IngestedFrom, @CycleTime, @LastUpdate, @Enabled, @RunNow)",
                new { IngestedFrom, CycleTime, LastUpdate, Enabled, RunNow })
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

            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"IngestCycles\" SET \"IngestedFrom\" = @IngestedFrom, \"CycleTime\" = @CycleTime, \"LastUpdate\" = @LastUpdate, \"Enabled\" = @Enabled, \"RunNow\" = @RunNow WHERE \"IngestCycleId\" = @IngestCycleId",
                new { IngestedFrom, CycleTime, LastUpdate, Enabled, RunNow, IngestCycleId })
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

            await using var connection = new NpgsqlConnection(connectionString);

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
