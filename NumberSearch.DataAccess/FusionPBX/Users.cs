using Dapper;

using Npgsql;

namespace NumberSearch.DataAccess.FusionPBX
{
    public readonly record struct User(
    Guid user_uuid,
    Guid? domain_uuid,
    string? username,
    string? password,
    string? salt,
    Guid? contact_uuid,
    string? user_status,
    string? api_key,
    string? user_enabled,
    string? add_user,
    string? add_date,
    string? user_email,
    DateTime? insert_date,
    Guid? insert_user,
    DateTime? update_date,
    Guid? update_user,
    string? user_totp_secret)
    {
        public static async Task<User[]> GetAllUsersAsync(ReadOnlyMemory<char> connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString.ToString());
            var result = await connection
                .QueryAsync<User>("SELECT * FROM v_users");

            return [.. result];
        }

        public static async Task<User> GetByApiKeyAsync(ReadOnlyMemory<char> apiKey, ReadOnlyMemory<char> connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString.ToString());
            var result = await connection
                .QueryFirstOrDefaultAsync<User>("SELECT * FROM v_users WHERE api_key = @api_key", new { api_key = apiKey.ToString() });

            return result;
        }
    }
}