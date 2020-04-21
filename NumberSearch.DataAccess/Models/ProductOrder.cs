using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    // Based on the schema discribed here: http://www.databaseanswers.org/data_models/customers_and_orders/index.htm
    public class ProductOrder
    {
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public Guid ServiceId { get; set; }
        public string DialedNumber { get; set; }
        public string PortedDialedNumber { get; set; }
        public int Quantity { get; set; }
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// Get all of the products adn their quantities associated with a specific order.
        /// </summary>
        /// <param name="OrderId"> The Id of the order. </param>
        /// <param name="connectionString"></param>
        /// <returns> A list of product related to an order. </returns>
        public static async Task<IEnumerable<ProductOrder>> GetAsync(Guid OrderId, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<ProductOrder>("SELECT \"OrderId\", \"ProductId\", \"ServiceId\", \"DialedNumber\", \"PortedDialedNumber\", \"Quantity\", \"CreateDate\" FROM public.\"ProductOrders\" " +
                "WHERE \"OrderId\" = @OrderId",
                new { OrderId })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Associate a specific product and quantity with and order.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PostAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            // Set the creation date to now.
            CreateDate = DateTime.Now;

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"ProductOrders\"(\"OrderId\", \"ProductId\", \"ServiceId\", \"DialedNumber\", \"PortedDialedNumber\", \"Quantity\", \"CreateDate\") " +
                "VALUES(@OrderId, @ProductId, @ServiceId, @DialedNumber, @PortedDialedNumber, @Quantity, @CreateDate)",
                new { OrderId, ProductId, ServiceId, DialedNumber, PortedDialedNumber, Quantity, CreateDate })
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
