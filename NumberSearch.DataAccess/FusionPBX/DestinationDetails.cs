using Dapper;

using Npgsql;

using NumberSearch.DataAccess.InvoiceNinja;

using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.FusionPBX
{
    public readonly record struct DestinationDetails(
    Guid destination_uuid,
    Guid? dialplan_uuid,
    Guid? fax_uuid,
    Guid? domain_uuid,
    string destination_type,
    string destination_number,
    string destination_number_regex,
    object destination_caller_id_name,
    object destination_caller_id_number,
    object destination_cid_name_prefix,
    string destination_context,
    object destination_record,
    object destination_accountcode,
    string destination_app,
    string destination_data,
    [property: JsonConverter(typeof(BooleanConverter))]
        bool destination_enabled,
    string destination_description,
    string destination_prefix,
    decimal destination_type_voice,
    decimal destination_type_fax,
    decimal destination_type_text,
    string destination_alternate_app,
    string destination_alternate_data,
    string destination_trunk_prefix,
    string destination_area_code,
    string destination_condition_field,
    string destination_hold_music,
    Guid? user_uuid,
    Guid? group_uuid,
    decimal destination_type_emergency,
    decimal destination_order,
    string destination_distinctive_ring,
    string destination_conditions,
    string destination_actions,
    DateTime? insert_date,
    Guid? insert_user,
    DateTime? update_date,
    Guid? update_user)
    {
        // FusionPBX generally stores destination numbers as plain 10 digit numbers like 2065551234,
        // so strip formatting and the NANP country code prefix from numbers like +12065551234 and 12065551234.
        public static string ToDestinationNumberFormat(ReadOnlyMemory<char> dialedNumber)
        {
            string digits = string.Concat(dialedNumber.ToString().Where(char.IsAsciiDigit));

            return digits.Length is 11 && digits[0] is '1' ? digits[1..] : digits;
        }

        public static async Task<DestinationDetails> GetByDialedNumberAsync(ReadOnlyMemory<char> dialedNumber, ReadOnlyMemory<char> connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString.ToString());
            string number = ToDestinationNumberFormat(dialedNumber);
            // Match on the trailing digits so that values stored with a prefix like 12065551234 or +12065551234 still match.
            DestinationDetails result = await connection
                .QueryFirstOrDefaultAsync<DestinationDetails>("SELECT * FROM v_destinations WHERE destination_number = @number OR destination_number LIKE '%' || @number", new { number });

            return result;
        }

        public static async Task<DestinationDetails> GetByDialedNumberAndDomainAsync(ReadOnlyMemory<char> dialedNumber, Guid domainUUID, ReadOnlyMemory<char> connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString.ToString());
            string number = ToDestinationNumberFormat(dialedNumber);
            DestinationDetails result = await connection
                .QueryFirstOrDefaultAsync<DestinationDetails>("SELECT * FROM v_destinations WHERE domain_uuid = @domainUUID AND (destination_number = @number OR destination_number LIKE '%' || @number)", new { domainUUID, number });

            return result;
        }

        public static async Task<DestinationDetails[]> GetByDomainIdAsync(Guid domainUUID, ReadOnlyMemory<char> connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString.ToString());
            var result = await connection
                .QueryAsync<DestinationDetails>("SELECT * FROM v_destinations WHERE domain_uuid = @domainUUID", new { domainUUID });

            return [.. result];
        }
    }

    public readonly record struct DomainDetails(
        Guid domain_uuid,
        Guid? domain_parent_uuid,
        string domain_name,
        bool domain_enabled,
        string domain_description,
        DateTime? insert_date,
        Guid? insert_user,
        DateTime? update_date,
        Guid? update_user
        )
    {
        public static async Task<DomainDetails> GetByDomainIdAsync(Guid domainUUID, ReadOnlyMemory<char> connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString.ToString());
            DomainDetails result = await connection
                .QueryFirstOrDefaultAsync<DomainDetails>("SELECT * FROM v_domains WHERE domain_uuid = @domainUUID", new { domainUUID });

            return result;
        }
    }

}