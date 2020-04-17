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
        public string BusinessName { get; set; }
        public string RoleTitle { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateSubmitted { get; set; }

        /// <summary>
        /// Get contacts by an email address.
        /// </summary>
        /// <param name="email"> An email address. </param>
        /// <param name="connectionString"> The database credentials. </param>
        /// <returns> One or more contacts. </returns>
        public static async Task<IEnumerable<ContactForm>> GetAsync(string email, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<ContactForm>("SELECT \"Id\",\"BusinessName\", \"RoleTitle\", \"FirstName\", \"LastName\", \"Email\", \"PhoneNumber\", \"DateSubmitted\" FROM public.\"SalesLeads\" " +
                "WHERE \"Email\" = @email",
                new { email })
                .ConfigureAwait(false);

            return result;
        }


        /// <summary>
        /// Submit a new contact to the database.
        /// </summary>
        /// <param name="connectionString"> The database credentials. </param>
        /// <returns> A success of failure indicator. </returns>
        public async Task<bool> PostAsync(string connectionString)
        {
            DateSubmitted = DateTime.Now;

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"SalesLeads\"(\"BusinessName\", \"RoleTitle\", \"FirstName\", \"LastName\", \"Email\", \"PhoneNumber\", \"DateSubmitted\") " +
                "VALUES(@BusinessName, @RoleTitle, @FirstName, @LastName, @Email, @PhoneNumber, @DateSubmitted)",
                new { BusinessName, RoleTitle, FirstName, LastName, Email, PhoneNumber, DateSubmitted })
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
