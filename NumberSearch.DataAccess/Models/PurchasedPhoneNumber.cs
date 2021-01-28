using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class PurchasedPhoneNumber
    {
        public Guid PurchasedPhoneNumberId { get; set; }
        public Guid OrderId { get; set; }
        public string DialedNumber { get; set; }
        public string IngestedFrom { get; set; }
        public DateTime DateIngested { get; set; }
        public DateTime DateOrdered { get; set; }
        public string OrderResponse { get; set; }
        public bool Completed { get; set; }
        public int NPA { get; set; }
        public int NXX { get; set; }
        public int XXXX { get; set; }
        public string NumberType { get; set; }
        public string PIN { get; set; }

        /// <summary>
        /// Get every purchased phone number.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PurchasedPhoneNumber>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<PurchasedPhoneNumber>("SELECT \"PurchasedPhoneNumberId\", \"OrderId\", \"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"DateOrdered\", \"OrderResponse\", \"Completed\", \"NPA\", \"NXX\", \"XXXX\", \"NumberType\", \"PIN\" FROM public.\"PurchasedPhoneNumbers\"")
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Get every purchased phone number.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<PurchasedPhoneNumber> GetByDialedNumberAndOrderIdAsync(string dialedNumber, Guid orderId, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<PurchasedPhoneNumber>("SELECT \"PurchasedPhoneNumberId\", \"OrderId\", \"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"DateOrdered\", \"OrderResponse\", \"Completed\", \"NPA\", \"NXX\", \"XXXX\", \"NumberType\", \"PIN\" FROM public.\"PurchasedPhoneNumbers\" WHERE \"DialedNumber\" = @dialedNumber AND \"OrderId\" = @orderId ", new { dialedNumber, orderId })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<PurchasedPhoneNumber>> GetByOrderIdAsync(Guid orderId, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<PurchasedPhoneNumber>("SELECT \"PurchasedPhoneNumberId\", \"OrderId\", \"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"DateOrdered\", \"OrderResponse\", \"Completed\", \"NPA\", \"NXX\", \"XXXX\", \"NumberType\", \"PIN\" FROM public.\"PurchasedPhoneNumbers\" WHERE \"OrderId\" = @orderId ", new { orderId })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Get every purchased phone number.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<PurchasedPhoneNumber> GetByDialedNumberAsync(string dialedNumber, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<PurchasedPhoneNumber>("SELECT \"PurchasedPhoneNumberId\", \"OrderId\", \"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"DateOrdered\", \"OrderResponse\", \"Completed\", \"NPA\", \"NXX\", \"XXXX\", \"NumberType\", \"PIN\" FROM public.\"PurchasedPhoneNumbers\" WHERE \"DialedNumber\" = @dialedNumber", new { dialedNumber })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Add a purchase record to the database for a phone number.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PostAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            // Set the creation date to now.
            DateOrdered = DateTime.Now;

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"PurchasedPhoneNumbers\"(\"OrderId\", \"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"DateOrdered\", \"OrderResponse\", \"Completed\", \"NPA\", \"NXX\", \"XXXX\", \"NumberType\", \"PIN\") VALUES (@OrderId, @DialedNumber, @IngestedFrom, @DateIngested, @DateOrdered, @OrderResponse, @Completed, @NPA, @NXX, @XXXX, @NumberType, @PIN)",
                new { OrderId, DialedNumber, IngestedFrom, DateIngested, DateOrdered, OrderResponse, Completed, NPA, NXX, XXXX, NumberType, PIN })
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
        /// Update a purchased phone number that already exists in the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PutAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"PurchasedPhoneNumbers\" SET \"OrderId\" = @OrderId, \"DialedNumber\" = @DialedNumber, \"IngestedFrom\" = @IngestedFrom, \"DateIngested\" = @DateIngested, \"DateOrdered\" = @DateOrdered, \"OrderResponse\" = @OrderResponse, \"Completed\" = @Completed, \"NPA\" = @NPA, \"NXX\" = @NXX, \"XXXX\" = @XXXX, \"NumberType\"= @NumberType, \"PIN\" = @PIN " +
                "WHERE \"PurchasedPhoneNumberId\" = @PurchasedPhoneNumberId",
                new { OrderId, DialedNumber, IngestedFrom, DateIngested, DateOrdered, OrderResponse, Completed, NPA, NXX, XXXX, NumberType, PIN, PurchasedPhoneNumberId })
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
        /// Delete a purchased phone number from the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(string connectionString)
        {
            if (PurchasedPhoneNumberId == Guid.Empty)
            {
                return false;
            }

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"PurchasedPhoneNumbers\" WHERE \"PurchasedPhoneNumberId\" = @PurchasedPhoneNumberId",
                new { PurchasedPhoneNumberId })
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