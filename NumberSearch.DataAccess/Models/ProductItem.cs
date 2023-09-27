using Dapper;

using Npgsql;
using NumberSearch.DataAccess;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccelerateNetworks.Operations
{
    public class ProductItem
    {
        public Guid ProductItemId { get; set; }
        public Guid ProductId { get; set; }
        public Guid? ProductShipmentId { get; set; }
        public Guid? OrderId { get; set; }
        public string? SerialNumber { get; set; } = null!;
        public string? MACAddress { get; set; } = null!;
        public string? Condition { get; set; } = null!;
        public string? ExternalOrderId { get; set; } = null!;
        public string? ShipmentTrackingLink { get; set; } = null!;
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public string? Carrier { get; set; } = null!;
        public string? TrackingNumber { get; set; } = null!;

        /// <summary>
        /// Get a list of products with the same name value.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<ProductItem>> GetByOrderIdAsync(Guid orderId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<ProductItem>("""SELECT "ProductItemId", "ProductId", "ProductShipmentId", "OrderId", "SerialNumber", "MACAddress", "Condition", "DateCreated", "DateUpdated", "ExternalOrderId", "ShipmentTrackingLink", "Carrier", "TrackingNumber" FROM public."ProductItems" """ +
                "WHERE \"OrderId\" = @orderId",
                new { orderId })
                .ConfigureAwait(false);

            return result;
        }
    }
}
