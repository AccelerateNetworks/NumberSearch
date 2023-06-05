using Flurl.Http;

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
        public string destination_actions { get; set; } = string.Empty;
        public string insert_date { get; set; } = string.Empty;
        public string insert_user { get; set; } = string.Empty;
        public string update_date { get; set; } = string.Empty;
        public string update_user { get; set; } = string.Empty;

        public static async Task<DestinationDetails> GetByDialedNumberAsync(string dialedNumber, string username, string password)
        {
            string baseUrl = "https://acceleratenetworks.sip.callpipe.com/app/rest_api/rest.php";
            string url = $"{baseUrl}";

            var response = await url
                .WithBasicAuth(username, password)
                .PostJsonAsync(new { action = "destination-details", number = dialedNumber });

            return await response.GetJsonAsync<DestinationDetails>();
        }
    }


    public class DomainDetails
    {
        public string domain_uuid { get; set; } = string.Empty;
        public string domain_parent_uuid { get; set; } = string.Empty;
        public string domain_name { get; set; } = string.Empty;
        public bool domain_enabled { get; set; } = false;
        public string domain_description { get; set; } = string.Empty;
        public string insert_date { get; set; } = string.Empty;
        public string insert_user { get; set; } = string.Empty;
        public string update_date { get; set; } = string.Empty;
        public string update_user { get; set; } = string.Empty;


        public static async Task<DomainDetails> GetByDomainIdAsync(string dialedNumber, string username, string password)
        {
            string baseUrl = "https://acceleratenetworks.sip.callpipe.com/app/rest_api/rest.php";
            string url = $"{baseUrl}";

            var response = await url
                .WithBasicAuth(username, password)
                .PostJsonAsync(new { action = "domain-details", domain_uuid = dialedNumber });

            return await response.GetJsonAsync<DomainDetails>();
        }
    }

}