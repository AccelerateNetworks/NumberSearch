using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class ProductShipment
    {
        public Guid ProductShipmentId { get; set; }
        public Guid ProductId { get; set; }
        public Guid OrderId { get; set; }
        public string BillingClientId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ShipmentSource { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        public string ShipmentType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Get a product by its Id.
        /// </summary>
        /// <param name="ProductId"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<ProductShipment?> GetByIdAsync(Guid productShipmentId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<ProductShipment>("SELECT \"ProductShipmentId\", \"ProductId\", \"OrderId\", \"BillingClientId\", \"Name\", \"ShipmentSource\", \"PurchasePrice\", \"ShipmentType\", \"Quantity\", \"DateCreated\" " +
                "FROM public.\"ProductShipments\" " +
                "WHERE \"ProductShipmentId\" = @productShipmentId",
                new { productShipmentId })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Get a list of products with the same name value.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<ProductShipment>> GetByProductIdAsync(Guid productId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<ProductShipment>("SELECT \"ProductShipmentId\", \"ProductId\", \"OrderId\", \"BillingClientId\", \"Name\", \"ShipmentSource\", \"PurchasePrice\", \"ShipmentType\", \"Quantity\", \"DateCreated\" " +
                "FROM public.\"ProductShipments\" " +
                "WHERE \"ProductId\" = @productId",
                new { productId })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Get a list of products with the same name value.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<ProductShipment>> GetByOrderIdAsync(Guid orderId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<ProductShipment>("SELECT \"ProductShipmentId\", \"ProductId\", \"OrderId\", \"BillingClientId\", \"Name\", \"ShipmentSource\", \"PurchasePrice\", \"ShipmentType\", \"Quantity\", \"DateCreated\" " +
                "FROM public.\"ProductShipments\" " +
                "WHERE \"OrderId\" = @orderId",
                new { orderId })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<ProductShipment>> GetAllAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<ProductShipment>("SELECT \"ProductShipmentId\", \"ProductId\", \"OrderId\", \"BillingClientId\", \"Name\", \"ShipmentSource\", \"PurchasePrice\", \"ShipmentType\", \"Quantity\", \"DateCreated\" " +
                "FROM public.\"ProductShipments\"")
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
                .ExecuteAsync("INSERT INTO public.\"ProductShipments\"(\"ProductShipmentId\", \"ProductId\", \"OrderId\", \"BillingClientId\", \"Name\", \"ShipmentSource\", \"PurchasePrice\", \"ShipmentType\", \"Quantity\", \"DateCreated\") " +
                "VALUES(@ProductShipmentId, @ProductId, @OrderId, @BillingClientId, @Name, @ShipmentSource, @PurchasePrice, @ShipmentType, @Quantity, @DateCreated)",
                new { ProductShipmentId, ProductId, OrderId, BillingClientId, Name, ShipmentSource, PurchasePrice, ShipmentType, Quantity, DateCreated })
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
        /// Update a product entry.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PutAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"ProductShipments\" SET \"ProductId\" = @ProductId, \"OrderId\" = @OrderId, \"BillingClientId\" = @BillingClientId, \"Name\" = @Name, \"ShipmentSource\" = @ShipmentSource, \"PurchasePrice\" = @PurchasePrice, \"ShipmentType\" = @ShipmentType, \"Quantity\" = @Quantity, \"DateCreated\" = @DateCreated " +
                "WHERE \"ProductShipmentId\" = @ProductShipmentId",
                new { ProductId, OrderId, BillingClientId, Name, ShipmentSource, PurchasePrice, ShipmentType, Quantity, DateCreated })
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

        public async Task<bool> DeleteAsync(string connectionString)
        {
            // Fail fast if we don have the primary key.
            if (ProductShipmentId == Guid.Empty)
            {
                return false;
            }

            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"ProductShipments\" WHERE \"ProductShipmentId\" = @ProductShipmentId",
                new { ProductShipmentId })
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
