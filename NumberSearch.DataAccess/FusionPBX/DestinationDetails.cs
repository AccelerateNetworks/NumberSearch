using Flurl.Http;

using NumberSearch.DataAccess.InvoiceNinja;

using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.FusionPBX
{
    public class DestinationDetails
    {
        public string domain_uuid { get; set; } = string.Empty;
        public string destination_uuid { get; set; } = string.Empty;
        public string dialplan_uuid { get; set; } = string.Empty;
        public object fax_uuid { get; set; } = string.Empty;
        public string destination_type { get; set; } = string.Empty;
        public string destination_number { get; set; } = string.Empty;
        public string destination_number_regex { get; set; } = string.Empty;
        public object destination_caller_id_name { get; set; } = string.Empty;
        public object destination_caller_id_number { get; set; } = string.Empty;
        public object destination_cid_name_prefix { get; set; } = string.Empty;
        public string destination_context { get; set; } = string.Empty;
        public object destination_record { get; set; } = string.Empty;
        public object destination_accountcode { get; set; } = string.Empty;
        public string destination_app { get; set; } = string.Empty;
        public string destination_data { get; set; } = string.Empty;
        [JsonConverter(typeof(BooleanConverter))]
        public bool destination_enabled { get; set; } = false;
        public string destination_description { get; set; } = string.Empty;
        public string destination_prefix { get; set; } = string.Empty;
        public string destination_type_voice { get; set; } = string.Empty;
        public string destination_type_fax { get; set; } = string.Empty;
        public string destination_type_text { get; set; } = string.Empty;
        public string destination_alternate_app { get; set; } = string.Empty;
        public string destination_alternate_data { get; set; } = string.Empty;
        public string destination_trunk_prefix { get; set; } = string.Empty;
        public string destination_area_code { get; set; } = string.Empty;
        public string destination_condition_field { get; set; } = string.Empty;
        public string destination_hold_music { get; set; } = string.Empty;
        public string user_uuid { get; set; } = string.Empty;
        public string group_uuid { get; set; } = string.Empty;
        public string destination_type_emergency { get; set; } = string.Empty;
        public string destination_order { get; set; } = string.Empty;
        public string destination_distinctive_ring { get; set; } = string.Empty;
        public string destination_conditions { get; set; } = string.Empty;
        public DestinationAction[] destination_actions { get; set; } = [];
        public string insert_date { get; set; } = string.Empty;
        public string insert_user { get; set; } = string.Empty;
        public string update_date { get; set; } = string.Empty;
        public string update_user { get; set; } = string.Empty;

        public readonly record struct DestinationAction(string destination_app,string destination_data);

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