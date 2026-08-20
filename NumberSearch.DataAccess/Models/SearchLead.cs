using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Models
{
    public class SearchLead
    {
        public Guid SearchLeadId { get; set; } = Guid.NewGuid();
        public string SessionId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ContactPhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EmailDomain { get; set; } = string.Empty;
        public bool MxRecordExists { get; set; }
        public bool ContactPhoneNumberPortable { get; set; }
        public string Query { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string ReverseDns { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string Referrer { get; set; } = string.Empty;
        public bool Blocked { get; set; }
        public string BlockReason { get; set; } = string.Empty;
        public DateTime DateSubmitted { get; set; } = DateTime.Now;

        private const string SelectColumns = "SELECT \"SearchLeadId\", \"SessionId\", \"Name\", \"ContactPhoneNumber\", \"Email\", \"EmailDomain\", \"MxRecordExists\", \"ContactPhoneNumberPortable\", \"Query\", \"IpAddress\", \"ReverseDns\", \"UserAgent\", \"Referrer\", \"Blocked\", \"BlockReason\", \"DateSubmitted\" FROM public.\"SearchLeads\"";

        public static async Task<IEnumerable<SearchLead>> GetAllAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryAsync<SearchLead>($"{SelectColumns} ORDER BY \"DateSubmitted\" DESC")
                .ConfigureAwait(false);
        }

        public static async Task<SearchLead?> GetAsync(Guid searchLeadId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryFirstOrDefaultAsync<SearchLead>($"{SelectColumns} WHERE \"SearchLeadId\" = @SearchLeadId", new { SearchLeadId = searchLeadId })
                .ConfigureAwait(false);
        }

        public static async Task<IEnumerable<SearchLead>> GetByEmailAsync(string email, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryAsync<SearchLead>($"{SelectColumns} WHERE LOWER(\"Email\") = LOWER(@Email) ORDER BY \"DateSubmitted\" DESC", new { Email = email })
                .ConfigureAwait(false);
        }

        public static async Task<IEnumerable<SearchLead>> GetBySessionAsync(string sessionId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryAsync<SearchLead>($"{SelectColumns} WHERE \"SessionId\" = @SessionId ORDER BY \"DateSubmitted\" DESC", new { SessionId = sessionId })
                .ConfigureAwait(false);
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"SearchLeads\" ( \"SearchLeadId\", \"SessionId\", \"Name\", \"ContactPhoneNumber\", \"Email\", \"EmailDomain\", \"MxRecordExists\", \"ContactPhoneNumberPortable\", \"Query\", \"IpAddress\", \"ReverseDns\", \"UserAgent\", \"Referrer\", \"Blocked\", \"BlockReason\", \"DateSubmitted\" ) " +
                "VALUES ( @SearchLeadId, @SessionId, @Name, @ContactPhoneNumber, @Email, @EmailDomain, @MxRecordExists, @ContactPhoneNumberPortable, @Query, @IpAddress, @ReverseDns, @UserAgent, @Referrer, @Blocked, @BlockReason, @DateSubmitted )",
                new { SearchLeadId, SessionId, Name, ContactPhoneNumber, Email, EmailDomain, MxRecordExists, ContactPhoneNumberPortable, Query, IpAddress, ReverseDns, UserAgent, Referrer, Blocked, BlockReason, DateSubmitted })
                .ConfigureAwait(false);

            return result == 1;
        }
    }
}
