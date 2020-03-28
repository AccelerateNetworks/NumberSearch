using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class ClientOnboarding
    {
        public Guid Id { get; set; }
        public string BusinessName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string ExpensivePhoneName { get; set; }
        public int EPCount { get; set; }
        public string CheapPhoneName { get; set; }
        public int CPCount { get; set; }
        public int LinesOrSeatsCount { get; set; }
        public bool Lines { get; set; }
        public bool Seats { get; set; }
        public int ExtraPhoneNumbers { get; set; }
        public bool FaxServer { get; set; }
        public DateTime DateSubmitted { get; set; }

        public static async Task<IEnumerable<ClientOnboarding>> GetAsync(string businessName, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"SELECT \"Id\", \"BusinessName\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"Country\", \"State\", \"Zip\", \"ExpensivePhoneName\", \"EPCount\", \"CheapPhoneName\", \"CPCount\", \"LinesOrSeatsCount\", \"Lines\", \"Seats\", \"ExtraPhoneNumbers\", \"FaxServer\", \"DateSubmitted\" FROM public.\"OnboardingOrders\" WHERE \"BusinessName\" = '{businessName}'";

            var result = await connection.QueryAsync<ClientOnboarding>(sql).ConfigureAwait(false);

            return result;
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            DateSubmitted = DateTime.Now;

            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"INSERT INTO public.\"OnboardingOrders\"(\"BusinessName\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"Country\", \"State\", \"Zip\", \"ExpensivePhoneName\", \"EPCount\", \"CheapPhoneName\", \"CPCount\", \"LinesOrSeatsCount\", \"Lines\", \"Seats\", \"ExtraPhoneNumbers\", \"FaxServer\", \"DateSubmitted\") VALUES ('{BusinessName}', '{FirstName}', '{LastName}', '{Email}', '{Address}', '{Address2}', '{Country}', '{State}', '{Zip}', '{ExpensivePhoneName}', {EPCount}, '{CheapPhoneName}', {CPCount}, {LinesOrSeatsCount}, {Lines}, {Seats}, {ExtraPhoneNumbers}, {FaxServer}, '{DateSubmitted}')";

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
