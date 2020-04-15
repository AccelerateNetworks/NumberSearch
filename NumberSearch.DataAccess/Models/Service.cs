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
        public string Name { get; set; }
        public int Price { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Get a product by its Id.
        /// </summary>
        /// <param name="ProductId"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<Service> GetAsync(Guid serviceId, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"SELECT \"ServiceId\", \"Name\", \"Price\", \"Description\" FROM public.\"Services\" WHERE \"ServiceId\" = '{serviceId}'";

            var result = await connection.QueryFirstOrDefaultAsync<Service>(sql).ConfigureAwait(false);

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
            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"SELECT \"ServiceId\", \"Name\", \"Price\", \"Description\" FROM public.\"Services\" WHERE \"Name\" = '{name}'";

            var result = await connection.QueryAsync<Service>(sql).ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<Service>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"SELECT \"ServiceId\", \"Name\", \"Price\", \"Description\" FROM public.\"Services\"";

            var result = await connection.QueryAsync<Service>(sql).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Associate a specific product and quantity with an order.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PostAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"INSERT INTO public.\"Services\"(\"Name\", \"Price\", \"Description\") VALUES('{Name}', '{Price}', '{Description}')";

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
