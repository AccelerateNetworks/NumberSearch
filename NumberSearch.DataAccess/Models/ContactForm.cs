using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class ContactForm
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateSubmitted { get; set; }

        public static async Task<IEnumerable<ContactForm>> GetAsync(string email, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"SELECT \"Id\", \"FirstName\", \"LastName\", \"Email\", \"PhoneNumber\", \"DateSubmitted\" FROM public.\"SalesLeads\" WHERE \"Email\" = '{email}'";

            var result = await connection.QueryAsync<ContactForm>(sql).ConfigureAwait(false);

            return result;
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            DateSubmitted = DateTime.Now;

            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"INSERT INTO public.\"SalesLeads\"(\"FirstName\", \"LastName\", \"Email\", \"PhoneNumber\", \"DateSubmitted\") VALUES('{FirstName}', '{LastName}', '{Email}', '{PhoneNumber}', '{DateSubmitted}')";

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
