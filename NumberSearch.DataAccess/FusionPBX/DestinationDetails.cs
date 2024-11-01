using Flurl.Http;

using NumberSearch.DataAccess.InvoiceNinja;

using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.FusionPBX
{
    public readonly record struct DestinationDetails(
        string destination_uuid,
        string dialplan_uuid,
        object fax_uuid,
        string domain_uuid,
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
        string destination_type_voice,
        string destination_type_fax,
        string destination_type_text,
        string destination_alternate_app,
        string destination_alternate_data,
        string destination_trunk_prefix,
        string destination_area_code,
        string destination_condition_field,
        string destination_hold_music,
        string user_uuid,
        string group_uuid,
        string destination_type_emergency,
        string destination_order,
        string destination_distinctive_ring,
        string destination_conditions,
        DestinationDetails.DestinationAction[] destination_actions,
        string insert_date,
        string insert_user,
        string update_date,
        string update_user)
    {

        public readonly record struct DestinationAction(string destination_app, string destination_data);

        public static async Task<DestinationDetails> GetByDialedNumberAsync(ReadOnlyMemory<char> dialedNumber, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            string baseUrl = "https://acceleratenetworks.sip.callpipe.com/app/rest_api/rest.php";
            string url = $"{baseUrl}";

            var response = await url
                .WithBasicAuth(username.ToString(), password.ToString())
                .PostJsonAsync(new { action = "destination-details", number = dialedNumber.ToString() });

            //var s = await response.GetStringAsync();

            return await response.GetJsonAsync<DestinationDetails>();
        }
    }

    public readonly record struct DomainDetails(
        string domain_uuid,
        string domain_parent_uuid,
        string domain_name,
        bool domain_enabled,
        string domain_description,
        string insert_date,
        string insert_user,
        string update_date,
        string update_user
        )
    {
        public static async Task<DomainDetails> GetByDomainIdAsync(ReadOnlyMemory<char> domainUUID, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            string baseUrl = "https://acceleratenetworks.sip.callpipe.com/app/rest_api/rest.php";
            string url = $"{baseUrl}";

            var response = await url
                .WithBasicAuth(username.ToString(), password.ToString())
                .PostJsonAsync(new { action = "domain-details", domain_uuid = domainUUID.ToString() });

            return await response.GetJsonAsync<DomainDetails>();
        }
    }

}