using Dapper;

using Npgsql;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class PhoneNumber
    {
        public string DialedNumber { get; set; }
        public int NPA { get; set; }
        public int NXX { get; set; }
        public int XXXX { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string IngestedFrom { get; set; }
        public DateTime DateIngested { get; set; }

        /// <summary>
        /// Get a list of all phone numbers in the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PhoneNumber>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            string sql = "SELECT \"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\" FROM public.\"PhoneNumbers\"";

            var result = await connection.QueryAsync<PhoneNumber>(sql).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Find a single phone number based on the complete number.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<PhoneNumber> GetAsync(string dialedNumber, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"SELECT \"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\" FROM public.\"PhoneNumbers\" WHERE \"DialedNumber\" = '{dialedNumber}'";

            var result = await connection.QuerySingleOrDefaultAsync<PhoneNumber>(sql).ConfigureAwait(false) ?? new PhoneNumber();

            return result;
        }

        /// <summary>
        /// Find a phone number based on matching it to a query.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PhoneNumber>> SearchAsync(string query, string connectionString)
        {
            // Convert stars to underscores which serve the same purpose as wildcards in PostgreSQL.
            query = query?.Trim()?.Replace('*', '_');

            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"SELECT \"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\" FROM public.\"PhoneNumbers\" WHERE \"DialedNumber\" LIKE '%{query}%'";

            var result = await connection.QueryAsync<PhoneNumber>(sql).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Find a phone number based on matching it to a query and returns paginated results.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PhoneNumber>> PaginatedSearchAsync(string query, int page, string connectionString)
        {
            var offset = (page * 100) - 100;
            var limit = 100;
            // Convert stars to underscores which serve the same purpose as wildcards in PostgreSQL.
            query = query?.Trim()?.Replace('*', '_');

            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"SELECT \"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\" FROM public.\"PhoneNumbers\" WHERE \"DialedNumber\" LIKE '%{query}%' ORDER BY \"DialedNumber\" OFFSET {offset} LIMIT {limit};";

            var result = await connection.QueryAsync<PhoneNumber>(sql).ConfigureAwait(false);

            return result;
        }

        public static async Task<int> NumberOfResultsInQuery(string query, string connectionString)
        {
            // Convert stars to underscores which serve the same purpose as wildcards in PostgreSQL.
            query = query?.Trim()?.Replace('*', '_');

            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"SELECT COUNT(*) FROM public.\"PhoneNumbers\" WHERE \"DialedNumber\" LIKE '%{query}%';";

            var result = await connection.QueryAsync<int>(sql).ConfigureAwait(false);

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Delete only numbers that haven't been reingested recently.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IngestStatistics> DeleteOld(DateTime ingestStart, string connectionString)
        {
            var start = DateTime.Now;

            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"DELETE FROM public.\"PhoneNumbers\" WHERE \"DateIngested\" < '{ingestStart.AddDays(-1)}'";

            var result = await connection.ExecuteAsync(sql).ConfigureAwait(false);

            return new IngestStatistics
            {
                Removed = result,
                IngestedFrom = "DatabaseCleanup",
                StartDate = start,
                EndDate = DateTime.Now
            };
        }


        /// <summary>
        /// Added new numbers to the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PostAsync(string connectionString)
        {
            // If anything is null bail out.
            if (NPA < 100 || NXX < 100 || XXXX < 1 || DialedNumber == null || City == null || State == null || IngestedFrom == null)
            {
                return false;
            }

            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"INSERT INTO public.\"PhoneNumbers\"(\"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\") VALUES('{DialedNumber}', {NPA}, {NXX}, {XXXX.ToString("0000", new CultureInfo("en-US"))}, '{City}', '{State}', '{IngestedFrom}', '{DateTime.Now}')";

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

        public static async Task<bool> BulkPostAsync(IEnumerable<PhoneNumber> numbers, string connectionString)
        {
            // Make sure there are some numbers incoming.
            if (numbers == null && numbers?.ToArray()?.Length < 0)
            {
                return false;
            }

            var values = new List<string>();

            foreach (var number in numbers?.ToArray())
            {
                // If anything is null bail out.
                if (!(number.NPA < 100 || number.NXX < 100 || number.XXXX < 1 || number.DialedNumber == null || number.City == null || number.State == null || number.IngestedFrom == null))
                {
                    values.Add($"('{number.DialedNumber}', {number.NPA}, {number.NXX}, {number.XXXX.ToString("0000", new CultureInfo("en-US"))}, '{number.City}', '{number.State}', '{number.IngestedFrom}', '{DateTime.Now}')");
                }
            }

            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"INSERT INTO public.\"PhoneNumbers\"(\"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\") VALUES";

            foreach (var value in values)
            {
                sql += $" {value},";
            }

            // Remove the extra comma.
            sql = sql.Substring(0, sql.Length - 1);

            var result = await connection.ExecuteAsync(sql).ConfigureAwait(false);

            if (result > 1)
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
            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"UPDATE public.\"PhoneNumbers\" SET \"City\" = '{City}', \"State\" = '{State}', \"IngestedFrom\" = '{IngestedFrom}', \"DateIngested\" = '{DateTime.Now}' WHERE \"DialedNumber\" = '{DialedNumber}'";

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

        /// <summary>
        /// Check if a specific phone number already exists in the database.
        /// </summary>
        /// <param name="postgresSQL"> The db connection string. </param>
        /// <returns> True if it does exist, and False if it doesn't. </returns>
        public bool ExistsInDb(Dictionary<string, PhoneNumber> existingNumbers)
        {
            var check = existingNumbers?.TryGetValue(DialedNumber, out PhoneNumber _) ?? false;

            return check;
        }
    }
}
