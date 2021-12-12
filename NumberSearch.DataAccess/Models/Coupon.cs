using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class Coupon
    {
        public Guid CouponId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public int Value { get; set; }
        public bool Public { get; set; }

        /// <summary>
        /// Get a coupon by its Id.
        /// </summary>
        /// <param name="couponId"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<Coupon> GetByIdAsync(Guid couponId, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<Coupon>("SELECT \"CouponId\", \"Name\", \"Description\", \"Public\", \"Type\", \"Value\" FROM public.\"Coupons\" " +
                "WHERE \"CouponId\" = @CouponId",
                new { CouponId = couponId })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Get all of the coupons.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<Coupon>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Coupon>("SELECT \"CouponId\", \"Name\", \"Description\", \"Public\", \"Type\", \"Value\"  FROM public.\"Coupons\"")
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Associate a specific coupon and quantity with an order.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PostAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"Coupons\"(\"CouponId\", \"Name\", \"Description\", \"Public\", \"Type\", \"Value\" ) " +
                "VALUES(@CouponId, @Name, @Description, @Public, @Type, @Value)",
                new { CouponId, Name, Description, Public, Type, Value })
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
        /// Update a coupon entry.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PutAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"Coupons\" SET \"Name\" = @Name, \"Description\" = @Description, \"Public\" = @Public, \"Type\" = @Type, \"Value\" = @Value " +
                "WHERE \"CouponId\" = @CouponId",
                new { Name, Description, Public, CouponId, Type, Value })
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
            //if (CouponId == Guid.Empty)
            //{
            //    return false;
            //}

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"Coupons\" WHERE \"CouponId\" = @CouponId",
                new { CouponId })
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
