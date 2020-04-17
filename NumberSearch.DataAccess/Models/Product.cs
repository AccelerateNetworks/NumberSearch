using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class Product
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }

        /// <summary>
        /// Get a product by its Id.
        /// </summary>
        /// <param name="ProductId"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<Product> GetAsync(Guid productId, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"SELECT \"ProductId\", \"Name\", \"Price\", \"Description\", \"Image\" FROM public.\"Products\" WHERE \"ProductId\" = '{productId}'";

            var result = await connection.QueryFirstOrDefaultAsync<Product>(sql).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Get a list of products with the same name value.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<Product>> GetAsync(string name, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"SELECT \"ProductId\", \"Name\", \"Price\", \"Description\", \"Image\" FROM public.\"Products\" WHERE \"Name\" = '{name}'";

            var result = await connection.QueryAsync<Product>(sql).ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<Product>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"SELECT \"ProductId\", \"Name\", \"Price\", \"Description\", \"Image\" FROM public.\"Products\"";

            var result = await connection.QueryAsync<Product>(sql).ConfigureAwait(false);

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

            string sql = $"INSERT INTO public.\"Products\"(\"Name\", \"Price\", \"Description\", \"Image\") VALUES('{Name}', '{Price}', '{Description}', '{Image}')";

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
