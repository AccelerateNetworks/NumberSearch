using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Text;
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

        /// <summary>
        /// Get every purchased phone number.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PurchasedPhoneNumber>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<PurchasedPhoneNumber>("SELECT \"PurchasedPhoneNumberId\", \"OrderId\", \"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"DateOrdered\", \"OrderResponse\", \"Completed\" FROM public.\"PurchasedPhoneNumbers\"")
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Get every purchased phone number.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<PurchasedPhoneNumber> GetByDialedNumberAsync(string dialedNumber, Guid orderId, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<PurchasedPhoneNumber>("SELECT \"PurchasedPhoneNumberId\", \"OrderId\", \"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"DateOrdered\", \"OrderResponse\", \"Completed\" FROM public.\"PurchasedPhoneNumbers\" WHERE \"DialedNumber\" = @dialedNumber AND \"OrderId\" = @orderId ", new { dialedNumber, orderId })
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
                .ExecuteAsync("INSERT INTO public.\"PurchasedPhoneNumbers\"(\"OrderId\", \"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"DateOrdered\", \"OrderResponse\", \"Completed\") VALUES(@OrderId, @DialedNumber, @IngestedFrom, @DateIngested, @DateOrdered, @OrderResponse, @Completed)",
                new { OrderId, DialedNumber, IngestedFrom, DateIngested, DateOrdered, OrderResponse, Completed })
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