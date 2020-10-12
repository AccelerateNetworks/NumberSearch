using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class OwnedPhoneNumber
    {
        public Guid OwnedPhoneNumberId { get; set; }
        public string DialedNumber { get; set; }
        public string IngestedFrom { get; set; }
        public DateTime DateIngested { get; set; }
        public bool Active { get; set; }
        public string BillingClientId { get; set; }
        public string OwnedBy { get; set; }
        public string Notes { get; set; }
        public string SPID { get; set; }
        public string SPIDName { get; set; }
        public string LIDBCNAM { get; set; }
        public Guid? EmergencyInformationId { get; set; }

        /// <summary>
        /// Get every owned phone number.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<OwnedPhoneNumber>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<OwnedPhoneNumber>("SELECT \"OwnedPhoneNumberId\", \"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"Active\", \"BillingClientId\", \"OwnedBy\", \"Notes\", \"SPID\", \"SPIDName\", \"LIDBCNAM\", \"EmergencyInformationId\" FROM public.\"OwnedPhoneNumbers\"")
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Get every purchased phone number.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<OwnedPhoneNumber> GetByDialedNumberAsync(string dialedNumber, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<OwnedPhoneNumber>("SELECT \"OwnedPhoneNumberId\", \"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"Active\", \"BillingClientId\", \"OwnedBy\", \"Notes\", \"SPID\", \"SPIDName\", \"LIDBCNAM\", \"EmergencyInformationId\" FROM public.\"OwnedPhoneNumbers\" WHERE \"DialedNumber\" = @dialedNumber", new { dialedNumber })
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
            DateIngested = DateTime.Now;

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"OwnedPhoneNumbers\" (\"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"Active\", \"BillingClientId\", \"OwnedBy\", \"Notes\", \"SPID\", \"SPIDName\", \"LIDBCNAM\", \"EmergencyInformationId\") VALUES (@DialedNumber, @IngestedFrom, @DateIngested, @Active, @BillingClientId, @OwnedBy, @Notes, @SPID, @SPIDName, @LIDBCNAM, @EmergencyInformationId)",
                new { DialedNumber, IngestedFrom, DateIngested, Active, BillingClientId, OwnedBy, Notes, SPID, SPIDName, LIDBCNAM, EmergencyInformationId })
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
        /// Add a purchase record to the database for a phone number.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PutAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            // Set the creation date to now.
            DateIngested = DateTime.Now;

            var result = await connection
                .ExecuteAsync("UPDATE public.\"OwnedPhoneNumbers\" SET \"IngestedFrom\" = @IngestedFrom, \"DateIngested\" = @DateIngested, \"Active\" = @Active, \"BillingClientId\" = @BillingClientId, \"OwnedBy\" = @OwnedBy, \"Notes\" = @Notes, \"SPID\" = @SPID, \"SPIDName\" = @SPIDName, \"LIDBCNAM\" = @LIDBCNAM, \"EmergencyInformationId\" = @EmergencyInformationId WHERE \"OwnedPhoneNumberId\" = @OwnedPhoneNumberId",
                new { IngestedFrom, DateIngested, Active, BillingClientId, OwnedBy, Notes, SPID, SPIDName, LIDBCNAM, OwnedPhoneNumberId, EmergencyInformationId })
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
            if (OwnedPhoneNumberId == Guid.Empty)
            {
                return false;
            }

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"OwnedPhoneNumbers\" WHERE \"OwnedPhoneNumberId\" = @OwnedPhoneNumberId",
                new { OwnedPhoneNumberId })
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