using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class OwnedPhoneNumber
    {
        public Guid OwnedPhoneNumberId { get; set; } = Guid.NewGuid();
        public string DialedNumber { get; set; } = string.Empty;
        public string IngestedFrom { get; set; } = string.Empty;
        public DateTime DateIngested { get; set; } = DateTime.Now;
        public bool Active { get; set; } = false;
        public string BillingClientId { get; set; } = string.Empty;
        public string OwnedBy { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string SPID { get; set; } = string.Empty;
        public string SPIDName { get; set; } = string.Empty;
        public string LIDBCNAM { get; set; } = string.Empty;
        public Guid? EmergencyInformationId { get; set; } = null;
        public DateTime DateUpdated { get; set; } = DateTime.Now;
        public string Status { get; set; } = string.Empty;
        public string FusionPBXClientId { get; set; } = string.Empty;
        public Guid? FPBXDomainId { get; set; } = null;
        public Guid? FPBXDestinationId { get; set; } = null;
        public string FPBXDomainName { get; set; } = string.Empty;
        public string FPBXDomainDescription { get; set; } = string.Empty;
        public string SMSRoute { get; set; } = string.Empty;
        public string TwilioCarrierName { get; set; } = string.Empty;
        public string TrunkGroup { get; set; } = string.Empty;

        /// <summary>
        /// Get every owned phone number.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<OwnedPhoneNumber>> GetAllAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<OwnedPhoneNumber>("""SELECT "OwnedPhoneNumberId", "DialedNumber", "IngestedFrom", "DateIngested", "Active", "BillingClientId", "OwnedBy", "Notes", "SPID", "SPIDName", "LIDBCNAM", "EmergencyInformationId", "DateUpdated", "Status", "FusionPBXClientId", "FPBXDomainId", "FPBXDestinationId", "FPBXDomainName", "FPBXDomainDescription", "SMSRoute", "TwilioCarrierName", "TrunkGroup" FROM public."OwnedPhoneNumbers" """)
                .ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Get every purchased phone number.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<OwnedPhoneNumber?> GetByDialedNumberAsync(string dialedNumber, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<OwnedPhoneNumber>("""SELECT "OwnedPhoneNumberId", "DialedNumber", "IngestedFrom", "DateIngested", "Active", "BillingClientId", "OwnedBy", "Notes", "SPID", "SPIDName", "LIDBCNAM", "EmergencyInformationId", "DateUpdated", "Status", "FusionPBXClientId", "FPBXDomainId", "FPBXDestinationId", "FPBXDomainName", "FPBXDomainDescription", "SMSRoute", "TwilioCarrierName", "TrunkGroup" FROM public."OwnedPhoneNumbers" WHERE "DialedNumber" = @dialedNumber""", new { dialedNumber })
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
            await using var connection = new NpgsqlConnection(connectionString);

            // Set the creation date to now.
            DateIngested = DateTime.Now;

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"OwnedPhoneNumbers\" (\"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"Active\", \"BillingClientId\", \"OwnedBy\", \"Notes\", \"SPID\", \"SPIDName\", \"LIDBCNAM\", \"EmergencyInformationId\", \"DateUpdated\", \"Status\", \"FusionPBXClientId\", \"FPBXDomainId\", \"FPBXDestinationId\", \"FPBXDomainName\", \"FPBXDomainDescription\", \"SMSRoute\", \"TwilioCarrierName\", \"TrunkGroup\") VALUES (@DialedNumber, @IngestedFrom, @DateIngested, @Active, @BillingClientId, @OwnedBy, @Notes, @SPID, @SPIDName, @LIDBCNAM, @EmergencyInformationId, @DateUpdated, @Status, @FusionPBXClientId, @FPBXDomainId, @FPBXDestinationId, @FPBXDomainName, @FPBXDomainDescription, @SMSRoute, @TwilioCarrierName, @TrunkGroup)",
                new { DialedNumber, IngestedFrom, DateIngested, Active, BillingClientId, OwnedBy, Notes, SPID, SPIDName, LIDBCNAM, EmergencyInformationId, DateUpdated, Status, FusionPBXClientId, FPBXDomainId, FPBXDestinationId, FPBXDomainName, FPBXDomainDescription, SMSRoute, TwilioCarrierName, TrunkGroup })
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
            await using var connection = new NpgsqlConnection(connectionString);

            // Set the creation date to now.
            DateUpdated = DateTime.Now;

            var result = await connection
                .ExecuteAsync("UPDATE public.\"OwnedPhoneNumbers\" SET \"IngestedFrom\" = @IngestedFrom, \"DateIngested\" = @DateIngested, \"Active\" = @Active, \"BillingClientId\" = @BillingClientId, \"OwnedBy\" = @OwnedBy, \"Notes\" = @Notes, \"SPID\" = @SPID, \"SPIDName\" = @SPIDName, \"LIDBCNAM\" = @LIDBCNAM, \"EmergencyInformationId\" = @EmergencyInformationId, \"DateUpdated\" = @DateUpdated, \"Status\" = @Status, \"FusionPBXClientId\" = @FusionPBXClientId, \"FPBXDomainId\" = @FPBXDomainId, \"FPBXDestinationId\" = @FPBXDestinationId, \"FPBXDomainName\" = @FPBXDomainName, \"FPBXDomainDescription\" = @FPBXDomainDescription, \"SMSRoute\" = @SMSRoute, \"TwilioCarrierName\" = @TwilioCarrierName, \"TrunkGroup\" = @TrunkGroup WHERE \"OwnedPhoneNumberId\" = @OwnedPhoneNumberId",
                new { IngestedFrom, DateIngested, Active, BillingClientId, OwnedBy, Notes, SPID, SPIDName, LIDBCNAM, EmergencyInformationId, DateUpdated, Status, FusionPBXClientId, FPBXDomainId, FPBXDestinationId, FPBXDomainName, FPBXDomainDescription, SMSRoute, TwilioCarrierName, TrunkGroup, OwnedPhoneNumberId })
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

            await using var connection = new NpgsqlConnection(connectionString);

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