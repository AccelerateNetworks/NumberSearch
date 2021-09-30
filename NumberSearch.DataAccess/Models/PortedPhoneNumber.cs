using Dapper;

using Npgsql;

using NumberSearch.DataAccess.BulkVS;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class PortedPhoneNumber
    {
        public Guid PortedPhoneNumberId { get; set; }
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
        public bool Wireless { get; set; }
        public string RequestStatus { get; set; }
        public DateTime? DateFirmOrderCommitment { get; set; }
        public string ExternalPortRequestId { get; set; }
        public bool Completed { get; set; }
        public string RawResponse { get; set; }
        // Only used by the porting process to get helpful information on the ported phone number.
        public LrnBulkCnam LrnLookup { get; set; }
        public bool Portable { get; set; }

        /// <summary>
        /// Get a list of all phone numbers in the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PortedPhoneNumber>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<PortedPhoneNumber>("SELECT \"PortedDialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"PortRequestId\", \"OrderId\", \"Wireless\", \"RequestStatus\", \"DateFirmOrderCommitment\", \"PortedPhoneNumberId\", \"ExternalPortRequestId\", \"Completed\", \"RawResponse\" " +
                "FROM public.\"PortedPhoneNumbers\"")
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
                .QueryAsync<PortedPhoneNumber>("SELECT \"PortedDialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"PortRequestId\", \"OrderId\", \"Wireless\", \"RequestStatus\", \"DateFirmOrderCommitment\", \"PortedPhoneNumberId\", \"ExternalPortRequestId\", \"Completed\", \"RawResponse\" " +
                "FROM public.\"PortedPhoneNumbers\" " +
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
                .QueryAsync<PortedPhoneNumber>("SELECT \"PortedDialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"PortRequestId\", \"OrderId\", \"Wireless\", \"RequestStatus\", \"DateFirmOrderCommitment\", \"PortedPhoneNumberId\", \"ExternalPortRequestId\", \"Completed\", \"RawResponse\" " +
                "FROM public.\"PortedPhoneNumbers\" " +
                "WHERE \"PortRequestId\" = @portRequestId", new { portRequestId })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Get a list of phone numbers with the same external port request id.
        /// </summary>
        /// <param name="externalPortRequestId"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PortedPhoneNumber>> GetByExternalIdAsync(string externalPortRequestId, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<PortedPhoneNumber>("SELECT \"PortedDialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"PortRequestId\", \"OrderId\", \"Wireless\", \"RequestStatus\", \"DateFirmOrderCommitment\", \"PortedPhoneNumberId\", \"ExternalPortRequestId\", \"Completed\", \"RawResponse\" " +
                "FROM public.\"PortedPhoneNumbers\" " +
                "WHERE \"ExternalPortRequestId\" = @externalPortRequestId", new { externalPortRequestId })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Find a single phone number based on the complete number.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PortedPhoneNumber>> GetByDialedNumberAsync(string dialedNumber, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<PortedPhoneNumber>("SELECT \"PortedDialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"PortRequestId\", \"OrderId\", \"Wireless\", \"RequestStatus\", \"DateFirmOrderCommitment\", \"PortedPhoneNumberId\", \"ExternalPortRequestId\", \"Completed\", \"RawResponse\" " +
                "FROM public.\"PortedPhoneNumbers\" " +
                "WHERE \"PortedDialedNumber\" = @dialedNumber", new { dialedNumber })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Find a single phone number based on the complete number.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<PortedPhoneNumber> GetByIdAsync(Guid PortedPhoneNumberId, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<PortedPhoneNumber>("SELECT \"PortedDialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"PortRequestId\", \"OrderId\", \"Wireless\", \"RequestStatus\", \"DateFirmOrderCommitment\", \"PortedPhoneNumberId\", \"ExternalPortRequestId\", \"Completed\", \"RawResponse\" " +
                "FROM public.\"PortedPhoneNumbers\" " +
                "WHERE \"PortedPhoneNumberId\" = @PortedPhoneNumberId", new { PortedPhoneNumberId })
                .ConfigureAwait(false);

            return result;
        }

        public async Task<bool> PutAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"PortedPhoneNumbers\" SET \"City\" = @City, \"State\" = @State, \"IngestedFrom\" = @IngestedFrom, \"DateIngested\" = @DateIngested, \"PortRequestId\" = @PortRequestId, \"OrderId\" = @OrderId, \"Wireless\" = @Wireless, \"RequestStatus\" = @RequestStatus, \"DateFirmOrderCommitment\" = @DateFirmOrderCommitment, \"ExternalPortRequestId\" = @ExternalPortRequestId, \"Completed\" = @Completed, \"RawResponse\" = @RawResponse " +
                "WHERE \"PortedPhoneNumberId\" = @PortedPhoneNumberId ",
                new { City, State, IngestedFrom, DateIngested = DateTime.Now, PortRequestId, OrderId, Wireless, RequestStatus, DateFirmOrderCommitment, ExternalPortRequestId, Completed, RawResponse, PortedPhoneNumberId })
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
                .ExecuteAsync("INSERT INTO public.\"PortedPhoneNumbers\"(\"PortedDialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"City\", \"State\", \"IngestedFrom\", \"DateIngested\", \"PortRequestId\", \"OrderId\", \"Wireless\", \"RequestStatus\", \"DateFirmOrderCommitment\", \"PortedPhoneNumberId\", \"ExternalPortRequestId\", \"Completed\", \"RawResponse\") " +
                "VALUES(@PortedDialedNumber, @NPA, @NXX, @XXXX, @City, @State, @IngestedFrom, @DateIngested, @PortRequestId, @OrderId, @Wireless, @RequestStatus, @DateFirmOrderCommitment, @PortedPhoneNumberId, @ExternalPortRequestId, @Completed, @RawResponse)",
                new { PortedDialedNumber, NPA, NXX, XXXX, City, State, IngestedFrom, DateIngested = DateTime.Now, PortRequestId, OrderId, Wireless, RequestStatus, DateFirmOrderCommitment, PortedPhoneNumberId, ExternalPortRequestId, Completed, RawResponse })
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
            if (PortedPhoneNumberId == Guid.Empty)
            {
                return false;
            }

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"PortedPhoneNumbers\" WHERE \"PortedPhoneNumberId\" = @PortedPhoneNumberId",
                new { PortedPhoneNumberId })
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