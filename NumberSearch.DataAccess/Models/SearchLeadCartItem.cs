using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Models
{
    public class SearchLeadCartItem
    {
        public Guid SearchLeadCartItemId { get; set; } = Guid.NewGuid();
        public Guid? SearchLeadId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string ProductIdentifier { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime DateAddedToCart { get; set; } = DateTime.Now;

        private const string SelectColumns = "SELECT \"SearchLeadCartItemId\", \"SearchLeadId\", \"SessionId\", \"ProductType\", \"ProductIdentifier\", \"Quantity\", \"DateAddedToCart\" FROM public.\"SearchLeadCartItems\"";

        public static async Task<IEnumerable<SearchLeadCartItem>> GetByLeadAsync(Guid searchLeadId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryAsync<SearchLeadCartItem>($"{SelectColumns} WHERE \"SearchLeadId\" = @SearchLeadId ORDER BY \"DateAddedToCart\"", new { SearchLeadId = searchLeadId })
                .ConfigureAwait(false);
        }

        public static async Task<IEnumerable<SearchLeadCartItem>> GetBySessionAsync(string sessionId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryAsync<SearchLeadCartItem>($"{SelectColumns} WHERE \"SessionId\" = @SessionId ORDER BY \"DateAddedToCart\"", new { SessionId = sessionId })
                .ConfigureAwait(false);
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"SearchLeadCartItems\" ( \"SearchLeadCartItemId\", \"SearchLeadId\", \"SessionId\", \"ProductType\", \"ProductIdentifier\", \"Quantity\", \"DateAddedToCart\" ) " +
                "VALUES ( @SearchLeadCartItemId, @SearchLeadId, @SessionId, @ProductType, @ProductIdentifier, @Quantity, @DateAddedToCart )",
                new { SearchLeadCartItemId, SearchLeadId, SessionId, ProductType, ProductIdentifier, Quantity, DateAddedToCart })
                .ConfigureAwait(false);

            return result == 1;
        }
    }
}
