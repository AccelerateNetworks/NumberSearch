using Dapper;

using Npgsql;

using System.Collections.Generic;
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

        public static async Task<IEnumerable<PhoneNumber>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            string sql = "SELECT \"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\" FROM public.\"PhoneNumbers\"";

            var result = await connection.QueryAsync<PhoneNumber>(sql);

            return result;
        }

        public static async Task<PhoneNumber> GetAsync(string dialedNumber, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"SELECT \"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\" FROM public.\"PhoneNumbers\" WHERE \"DialedNumber\" = '{dialedNumber}'";

            var result = await connection.QuerySingleOrDefaultAsync<PhoneNumber>(sql) ?? new PhoneNumber();

            return result;
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            // If anything is null bail out.
            if (NPA < 100 || NXX < 100 || XXXX < 1 || DialedNumber == null || City == null || State == null || IngestedFrom == null)
            {
                return false;
            }

            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"INSERT INTO public.\"PhoneNumbers\"(\"DialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\") VALUES('{DialedNumber}', {NPA}, {NXX}, {XXXX.ToString("0000")}, '{City}', '{State}', '{IngestedFrom}')";

            var result = await connection.ExecuteAsync(sql);

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
            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"UPDATE public.\"PhoneNumbers\" SET \"City\" = '{City}', \"State\" = '{State}', \"IngestedFrom\" = '{IngestedFrom}' WHERE \"DialedNumber\" = '{DialedNumber}'";

            var result = await connection.ExecuteAsync(sql);

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
        public async Task<bool> ExistsInDb(string connectionString)
        {
            var result = await PhoneNumber.GetAsync(DialedNumber, connectionString);

            if (string.IsNullOrWhiteSpace(result.DialedNumber))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
