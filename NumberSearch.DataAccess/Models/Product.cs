using Dapper;

using Npgsql;

using NumberSearch.DataAccess.TeleDynamics;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class Product
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public bool Public { get; set; }
        public int QuantityAvailable { get; set; }
        public string SupportLink { get; set; } = string.Empty;
        public int DisplayPriority { get; set; }
        public string VendorPartNumber { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string VendorDescription { get; set; } = string.Empty;
        public string MarkdownContent { get; set; } = string.Empty;
        public string VendorFeatures { get; set; } = string.Empty;
        public decimal InstallTime { get; set; } = 0m;
        // Just for the Price list page
        public VendorProduct Vendor { get; set; } = new();

        /// <summary>
        /// Get a product by its Id.
        /// </summary>
        /// <param name="ProductId"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<Product?> GetByIdAsync(Guid productId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<Product>("SELECT \"ProductId\", \"Name\", \"Price\", \"Description\", \"Image\", \"Public\", \"QuantityAvailable\", \"SupportLink\", \"DisplayPriority\", \"VendorPartNumber\", \"Type\", \"Tags\", \"VendorDescription\", \"VendorFeatures\", \"MarkdownContent\", \"InstallTime\" FROM public.\"Products\" " +
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
        public static async Task<IEnumerable<Product>> GetAsync(string name, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Product>("SELECT \"ProductId\", \"Name\", \"Price\", \"Description\", \"Image\", \"Public\", \"QuantityAvailable\", \"SupportLink\", \"DisplayPriority\", \"VendorPartNumber\", \"Type\", \"Tags\", \"VendorDescription\", \"VendorFeatures\", \"MarkdownContent\", \"InstallTime\"  FROM public.\"Products\" " +
                "WHERE \"Name\" = @name",
                new { name })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<Product>> GetAllAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Product>("SELECT \"ProductId\", \"Name\", \"Price\", \"Description\", \"Image\", \"Public\", \"QuantityAvailable\", \"SupportLink\", \"DisplayPriority\", \"VendorPartNumber\", \"Type\", \"Tags\", \"VendorDescription\", \"VendorFeatures\", \"MarkdownContent\", \"InstallTime\" FROM public.\"Products\"")
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
                .ExecuteAsync("INSERT INTO public.\"Products\"(\"Name\", \"Price\", \"Description\", \"Image\", \"Public\", \"QuantityAvailable\", \"SupportLink\", \"DisplayPriority\", \"VendorPartNumber\", \"Type\", \"Tags\", \"VendorDescription\", \"VendorFeatures\", \"MarkdownContent\", \"InstallTime\") " +
                "VALUES(@Name, @Price, @Description, @Image, @Public, @QuantityAvailable, @SupportLink, @DisplayPriority, @VendorPartNumber, @Type, @Tags, @VendorDescription, @VendorFeatures, @MarkdownContent, @InstallTime)",
                new { Name, Price, Description, Image, Public, QuantityAvailable, SupportLink, DisplayPriority, VendorPartNumber, Type, Tags, VendorDescription, VendorFeatures, MarkdownContent, InstallTime })
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
                .ExecuteAsync("UPDATE public.\"Products\" " +
                "SET \"Name\" = @Name, \"Price\" = @Price, \"Description\" = @Description, \"Image\" = @Image, \"Public\" = @Public, \"QuantityAvailable\" = @QuantityAvailable, \"SupportLink\" = @SupportLink, \"DisplayPriority\" = @DisplayPriority, \"VendorPartNumber\" = @VendorPartNumber, \"Type\" = @Type, \"Tags\" = @Tags, \"VendorDescription\" = @VendorDescription, \"VendorFeatures\" = @VendorFeatures, \"MarkdownContent\" = @MarkdownContent, \"InstallTime\" = @InstallTime " +
                "WHERE \"ProductId\" = @ProductId",
                new { Name, Price, Description, Image, Public, ProductId, QuantityAvailable, SupportLink, DisplayPriority, VendorPartNumber, Type, Tags, VendorDescription, VendorFeatures, MarkdownContent, InstallTime })
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
            if (ProductId == Guid.Empty)
            {
                return false;
            }

            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"Products\" WHERE \"ProductId\" = @ProductId",
                new { ProductId })
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
