using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class EmergencyInformation
    {
        public Guid EmergencyInformationId { get; set; } = Guid.NewGuid();
        public string DialedNumber { get; set; } = string.Empty;
        public string IngestedFrom { get; set; } = string.Empty;
        public DateTime DateIngested { get; set; } = DateTime.Now;
        public string CallerName { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string AddressLine2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string Sms { get; set; } = string.Empty;
        public string RawResponse { get; set; } = string.Empty;
        public DateTime? BulkVSLastModificationDate { get; set; }
        public DateTime? ModifiedDate { get; set; }


        public static async Task<IEnumerable<EmergencyInformation>> GetAllAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryAsync<EmergencyInformation>("""SELECT "EmergencyInformationId", "DialedNumber", "IngestedFrom", "DateIngested", "CallerName", "AddressLine1", "AddressLine2", "City", "State", "Zip", "Sms", "BulkVSLastModificationDate", "ModifiedDate", "RawResponse" FROM public."EmergencyInformation" """)
                .ConfigureAwait(false);
        }

        public static async Task<EmergencyInformation?> GetByIdAsync(Guid EmergencyInformationId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryFirstOrDefaultAsync<EmergencyInformation>("SELECT \"EmergencyInformationId\", \"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"CallerName\", \"AddressLine1\", \"AddressLine2\", \"City\", \"State\", \"Zip\", \"Sms\", \"BulkVSLastModificationDate\", \"ModifiedDate\", \"RawResponse\" FROM public.\"EmergencyInformation\" " +
                "WHERE \"EmergencyInformationId\" = @EmergencyInformationId", new { EmergencyInformationId })
                .ConfigureAwait(false);
        }

        public static async Task<IEnumerable<EmergencyInformation>> GetByDialedNumberAsync(string dialedNumber, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryAsync<EmergencyInformation>("SELECT \"EmergencyInformationId\", \"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"CallerName\", \"AddressLine1\", \"AddressLine2\", \"City\", \"State\", \"Zip\", \"Sms\", \"BulkVSLastModificationDate\", \"ModifiedDate\", \"RawResponse\" FROM public.\"EmergencyInformation\" WHERE \"DialedNumber\" = @dialedNumber", new { dialedNumber })
                .ConfigureAwait(false);
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"EmergencyInformation\" (\"EmergencyInformationId\", \"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"CallerName\", \"AddressLine1\", \"AddressLine2\", \"City\", \"State\", \"Zip\", \"Sms\", \"BulkVSLastModificationDate\", \"ModifiedDate\", \"RawResponse\") " +
                "VALUES ( @EmergencyInformationId, @DialedNumber, @IngestedFrom, @DateIngested, @CallerName, @AddressLine1, @AddressLine2, @City, @State, @Zip, @Sms,  @BulkVSLastModificationDate, @ModifiedDate, @RawResponse )",
                new { EmergencyInformationId, DialedNumber, IngestedFrom, DateIngested, CallerName, AddressLine1, AddressLine2, City, State, Zip, Sms, BulkVSLastModificationDate, ModifiedDate, RawResponse })
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

        public async Task<bool> PutAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"EmergencyInformation\" SET \"EmergencyInformationId\"=@EmergencyInformationId, \"DialedNumber\"=@DialedNumber, \"IngestedFrom\"=@IngestedFrom, \"DateIngested\"=@DateIngested, \"CallerName\"=@CallerName, \"AddressLine1\"=@AddressLine1, \"AddressLine2\"=@AddressLine2, \"City\"=@City, \"State\"=@State, \"Zip\"=@Zip, \"Sms\"=@Sms, \"BulkVSLastModificationDate\"=@BulkVSLastModificationDate, \"ModifiedDate\"=@ModifiedDate, \"RawResponse\"=@RawResponse WHERE \"EmergencyInformationId\" = @EmergencyInformationId",
                new { DialedNumber, IngestedFrom, DateIngested, CallerName, AddressLine1, AddressLine2, City, State, Zip, Sms, BulkVSLastModificationDate, ModifiedDate, RawResponse, EmergencyInformationId })
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
            if (EmergencyInformationId == Guid.Empty)
            {
                return false;
            }

            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"EmergencyInformation\" WHERE \"EmergencyInformationId\" = @EmergencyInformationId",
                new { EmergencyInformationId })
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
