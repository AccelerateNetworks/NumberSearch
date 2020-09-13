using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class PortedPhoneNumber
    {
        public string PortedDialedNumber { get; set; }
        public int NPA { get; set; }
        public int NXX { get; set; }
        public int XXXX { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string IngestedFrom { get; set; }
        public DateTime DateIngested { get; set; }
        public Guid? PortRequestId { get; set; }
        public Guid? OrderId { get; set; }

        /// <summary>
        /// Get a list of all phone numbers in the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PortedPhoneNumber>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<PortedPhoneNumber>("SELECT \"PortedDialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"PortRequestId\", \"OrderId\" FROM public.\"PortedPhoneNumbers\"")
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Find a single phone number based on the complete number.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PortedPhoneNumber>> GetByOrderIdAsync(Guid orderId, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<PortedPhoneNumber>("SELECT \"PortedDialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"PortRequestId\", \"OrderId\" FROM public.\"PortedPhoneNumbers\" " +
                "WHERE \"OrderId\" = @orderId", new { orderId })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Find a single phone number based on the complete number.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PortedPhoneNumber>> GetByPortRequestIdAsync(Guid portRequestId, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<PortedPhoneNumber>("SELECT \"PortedDialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"PortRequestId\", \"OrderId\" FROM public.\"PortedPhoneNumbers\" " +
                "WHERE \"PortRequestId\" = @portRequestId", new { portRequestId })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Find a single phone number based on the complete number.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<PortedPhoneNumber> GetByDialedNumberAsync(string dialedNumber, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<PortedPhoneNumber>("SELECT \"PortedDialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"PortRequestId\", \"OrderId\" FROM public.\"PortedPhoneNumbers\" " +
                "WHERE \"PortedDialedNumber\" = @dialedNumber", new { dialedNumber })
                .ConfigureAwait(false);

            return result;
        }

        public async Task<bool> PutAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"PortedPhoneNumbers\" SET \"City\" = @City, \"State\" = @State, \"IngestedFrom\" = @IngestedFrom, \"DateIngested\" = @DateIngested, \"PortRequestId\" = @PortRequestId, \"OrderId\" = @OrderId WHERE \"PortedDialedNumber\" = @PortedDialedNumber",
                new { City, State, IngestedFrom, DateIngested = DateTime.Now, PortRequestId, OrderId, PortedDialedNumber })
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
        /// Added new numbers to the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PostAsync(string connectionString)
        {
            // If anything is null bail out.
            if (NPA < 100 || NXX < 100 || XXXX < 1 || PortedDialedNumber == null || City == null || State == null || IngestedFrom == null)
            {
                return false;
            }

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"PortedPhoneNumbers\"(\"PortedDialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"PortRequestId\", \"OrderId\") " +
                "VALUES(@PortedDialedNumber, @NPA, @NXX, @XXXX, @City, @State, @IngestedFrom, @DateIngested, @PortRequestId, @OrderId)",
                new { PortedDialedNumber, NPA, NXX, XXXX, City, State, IngestedFrom, DateIngested = DateTime.Now, PortRequestId, OrderId })
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