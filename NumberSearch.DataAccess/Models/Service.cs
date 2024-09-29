using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class Service
    {
        public Guid ServiceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Price { get; set; }
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Get a product by its Id.
        /// </summary>
        /// <param name="ProductId"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<Service?> GetAsync(Guid serviceId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<Service>("SELECT \"ServiceId\", \"Name\", \"Price\", \"Description\" FROM public.\"Services\" " +
                "WHERE \"ServiceId\" = @serviceId",
                new { serviceId })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Get a list of products with the same name value.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<Service>> GetAsync(string name, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Service>("SELECT \"ServiceId\", \"Name\", \"Price\", \"Description\" FROM public.\"Services\" " +
                "WHERE \"Name\" = @name",
                new { name })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<Service>> GetAllAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Service>("SELECT \"ServiceId\", \"Name\", \"Price\", \"Description\" FROM public.\"Services\"")
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Associate a specific product and quantity with an order.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"Services\"(\"Name\", \"Price\", \"Description\") " +
                "VALUES(@Name, @Price, @Description)",
                new { Name, Price, Description })
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
