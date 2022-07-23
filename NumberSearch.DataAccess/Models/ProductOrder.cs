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
        public Guid ProductOrderId { get; set; }
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public Guid ServiceId { get; set; }
        public string DialedNumber { get; set; }
        public string PortedDialedNumber { get; set; }
        public int Quantity { get; set; }
        public DateTime CreateDate { get; set; }
        public Guid? PortedPhoneNumberId { get; set; }
        public Guid? VerifiedPhoneNumberId { get; set; }
        public Guid? CouponId { get; set; }

        public static async Task<IEnumerable<ProductOrder>> GetAllAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<ProductOrder>
                ("SELECT \"ProductOrderId\", \"OrderId\", \"ProductId\", \"ServiceId\", \"DialedNumber\", \"PortedDialedNumber\", \"Quantity\", \"CreateDate\", \"PortedPhoneNumberId\", \"VerifiedPhoneNumberId\", \"CouponId\" FROM public.\"ProductOrders\"")
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Get all of the products adn their quantities associated with a specific order.
        /// </summary>
        /// <param name="OrderId"> The Id of the order. </param>
        /// <param name="connectionString"></param>
        /// <returns> A list of product related to an order. </returns>
        public static async Task<IEnumerable<ProductOrder>> GetAsync(Guid OrderId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<ProductOrder>("SELECT \"ProductOrderId\", \"OrderId\", \"ProductId\", \"ServiceId\", \"DialedNumber\", \"PortedDialedNumber\", \"Quantity\", \"CreateDate\", \"PortedPhoneNumberId\", \"VerifiedPhoneNumberId\", \"CouponId\" FROM public.\"ProductOrders\" " +
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
            await using var connection = new NpgsqlConnection(connectionString);

            // Set the creation date to now.
            CreateDate = DateTime.Now;

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"ProductOrders\"( \"ProductOrderId\", \"OrderId\", \"ProductId\", \"ServiceId\", \"DialedNumber\", \"PortedDialedNumber\", \"Quantity\", \"CreateDate\", \"PortedPhoneNumberId\", \"VerifiedPhoneNumberId\", \"CouponId\") " +
                "VALUES( @ProductOrderId, @OrderId, @ProductId, @ServiceId, @DialedNumber, @PortedDialedNumber, @Quantity, @CreateDate, @PortedPhoneNumberId, @VerifiedPhoneNumberId, @CouponId)",
                new { ProductOrderId, OrderId, ProductId, ServiceId, DialedNumber, PortedDialedNumber, Quantity, CreateDate, PortedPhoneNumberId, VerifiedPhoneNumberId, CouponId })
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

        public async Task<bool> PutAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"ProductOrders\" " +
                "SET \"OrderId\" = @OrderId, \"ProductId\" = @ProductId, \"ServiceId\" = @ServiceId, \"DialedNumber\" = @DialedNumber, \"PortedDialedNumber\" = @PortedDialedNumber, \"Quantity\" = @Quantity, \"CreateDate\" = @CreateDate, \"PortedPhoneNumberId\" = @PortedPhoneNumberId, \"VerifiedPhoneNumberId\" = @VerifiedPhoneNumberId, \"CouponId\" = @CouponId " +
                "WHERE \"ProductOrderId\" = @ProductOrderId",
                new { OrderId, ProductId, ServiceId, DialedNumber, PortedDialedNumber, Quantity, CreateDate, PortedPhoneNumberId, VerifiedPhoneNumberId, CouponId, ProductOrderId })
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

        /// <summary>
        /// Delete all of the product orders related to a specific order from the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> DeleteByOrderAsync(string connectionString)
        {
            if (OrderId == Guid.Empty)
            {
                return false;
            }

            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"ProductOrders\" WHERE \"OrderId\" = @OrderId",
                new { OrderId })
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

        /// <summary>
        /// Delete a specific product order using its Id.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> DeleteByIdAsync(string connectionString)
        {
            if (ProductOrderId == Guid.Empty)
            {
                return false;
            }

            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"ProductOrders\" WHERE \"ProductOrderId\" = @ProductOrderId",
                new { ProductOrderId })
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
