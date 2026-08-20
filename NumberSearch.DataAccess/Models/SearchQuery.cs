using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Models
{
    public class SearchQuery
    {
        public Guid SearchQueryId { get; set; } = Guid.NewGuid();
        public Guid? SearchLeadId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactPhoneNumber { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime DateSearched { get; set; } = DateTime.Now;

        private const string SelectColumns = "SELECT \"SearchQueryId\", \"SearchLeadId\", \"SessionId\", \"Query\", \"Email\", \"ContactPhoneNumber\", \"IpAddress\", \"UserAgent\", \"DateSearched\" FROM public.\"SearchQueries\"";

        public static async Task<IEnumerable<SearchQuery>> GetByQueryAsync(string query, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryAsync<SearchQuery>($"{SelectColumns} WHERE \"Query\" = @Query ORDER BY \"DateSearched\" DESC", new { Query = query })
                .ConfigureAwait(false);
        }

        public static async Task<IEnumerable<SearchQuery>> GetByEmailAsync(string email, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryAsync<SearchQuery>($"{SelectColumns} WHERE LOWER(\"Email\") = LOWER(@Email) ORDER BY \"DateSearched\" DESC", new { Email = email })
                .ConfigureAwait(false);
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"SearchQueries\" ( \"SearchQueryId\", \"SearchLeadId\", \"SessionId\", \"Query\", \"Email\", \"ContactPhoneNumber\", \"IpAddress\", \"UserAgent\", \"DateSearched\" ) " +
                "VALUES ( @SearchQueryId, @SearchLeadId, @SessionId, @Query, @Email, @ContactPhoneNumber, @IpAddress, @UserAgent, @DateSearched )",
                new { SearchQueryId, SearchLeadId, SessionId, Query, Email, ContactPhoneNumber, IpAddress, UserAgent, DateSearched })
                .ConfigureAwait(false);

            return result == 1;
        }
    }
}
