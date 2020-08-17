using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class IngestCycle
    {
        public Guid? Id { get; set; }
        public DateTime CycleTime { get; set; }
        public bool Enabled { get; set; }
        public string IngestedFrom { get; set; }
        public DateTime LastUpdate { get; set; }

        public static async Task<IEnumerable<IngestStatistics>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<IngestStatistics>("SELECT \"Id\", \"NumbersRetrived\", \"IngestedNew\", \"FailedToIngest\", \"UpdatedExisting\", \"Unchanged\", \"Removed\", \"IngestedFrom\", \"StartDate\", \"EndDate\", \"Lock\" FROM public.\"Ingests\" ORDER BY \"StartDate\" DESC")
                .ConfigureAwait(false);

            return result;
        }
    }
}
