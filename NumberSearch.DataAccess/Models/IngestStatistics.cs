using Dapper;

using Npgsql;

using System;
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
        public string IngestedFrom { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public async Task<bool> PostAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"INSERT INTO public.\"Ingests\"( \"NumbersRetrived\", \"IngestedNew\", \"FailedToIngest\", \"UpdatedExisting\", \"Unchanged\", \"IngestedFrom\", \"StartDate\", \"EndDate\") VALUES ({NumbersRetrived}, {IngestedNew}, {FailedToIngest}, {UpdatedExisting}, {Unchanged}, '{IngestedFrom}', '{StartDate}', '{EndDate}')";

            var result = await connection.ExecuteAsync(sql).ConfigureAwait(false);

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
