using Flurl.Http;

using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.InvoiceNinja
{
    public class Client
    {
        public ClientDatum[] data { get; set; }

        // Disabled because the Array versus object formatting for the ClientLinks is inconsistent and breaks the JSON serialization.
        // And we don't need this data because we're not a JavaScript front-end client.
        //public ClientMeta meta { get; set; }

        public static async Task<Client> GetAllClientsAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "clients";
            string tokenHeader = "X-Api-Token";
            string perPageParameter = "?per_page=10000";
            string url = $"{baseUrl}{endpoint}{perPageParameter}";

            return await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<Client>()
                .ConfigureAwait(false);
        }

        public static async Task<Client> GetByEmailAsync(string email, string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "clients";
            string tokenHeader = "X-Api-Token";
            string emailParameter = $"?email={email}";
            string url = $"{baseUrl}{endpoint}{emailParameter}";

            return await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<Client>()
                .ConfigureAwait(false);
        }

        public static async Task<ClientDatum> GetByIdAsync(string clientId, string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "clients";
            string tokenHeader = "X-Api-Token";
            string clientIdParameter = $"/{clientId}";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<ClientSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }
    }

    /// <summary>
    /// This DTO exists soley to support the GetByIdAsync method.
    /// The JSON body returned by the API is a JSON object with a single field called "data" that contains a single object "Datum".
    /// Which is just different enough from the standard JSON body that we need this DTO to handle it.
    /// </summary>
    public class ClientSingle
    {
        public ClientDatum data { get; set; }
    }

    public class ClientDatum
    {

        public string id { get; set; }
        public string user_id { get; set; }
        public string assigned_user_id { get; set; }
        public string name { get; set; }
        public string website { get; set; }
        public string private_notes { get; set; }
        public decimal balance { get; set; }
        public string group_settings_id { get; set; }
        public decimal paid_to_date { get; set; }
        public decimal credit_balance { get; set; }
        public int last_login { get; set; }
        public string size_id { get; set; }
        public string public_notes { get; set; }
        public string client_hash { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string phone { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string postal_code { get; set; }
        public string country_id { get; set; }
        public string industry_id { get; set; }
        public string custom_value1 { get; set; }
        public string custom_value2 { get; set; }
        public string custom_value3 { get; set; }
        public string custom_value4 { get; set; }
        public string shipping_address1 { get; set; }
        public string shipping_address2 { get; set; }
        public string shipping_city { get; set; }
        public string shipping_state { get; set; }
        public string shipping_postal_code { get; set; }
        public string shipping_country_id { get; set; }
        public ClientSettings settings { get; set; }
        public bool is_deleted { get; set; }
        public string vat_number { get; set; }
        public string id_number { get; set; }
        public int updated_at { get; set; }
        public int archived_at { get; set; }
        public int created_at { get; set; }
        public string display_name { get; set; }
        public string number { get; set; }
        public ClientContact[] contacts { get; set; }
        public object[] documents { get; set; }
        public ClientGateway_Tokens[] gateway_tokens { get; set; }
        public InvoiceDatum[] invoices { get; set; }

        public async Task<ClientDatum> PostAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "clients";
            string tokenHeader = "X-Api-Token";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string url = $"{baseUrl}{endpoint}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .WithHeader(contentHeader, contentHeaderValue)
                .PostJsonAsync(new { name })
                .ReceiveJson<ClientSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public async Task<ClientDatum> PutAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "clients";
            string tokenHeader = "X-Api-Token";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string clientIdParameter = $"/{id}";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .WithHeader(contentHeader, contentHeaderValue)
                .PutJsonAsync(new { name, contacts, address1, address2, city, state, postal_code })
                .ReceiveJson<ClientSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public async Task<ClientDatum> DeleteAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "clients";
            string tokenHeader = "X-Api-Token";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string clientIdParameter = $"/{id}";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .WithHeader(contentHeader, contentHeaderValue)
                .DeleteAsync()
                .ReceiveJson<ClientSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }
    }


    public class ClientSettings
    {
        public string currency_id { get; set; }
    }

    public class ClientContact
    {
        public string id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public int created_at { get; set; }
        public int updated_at { get; set; }
        public int archived_at { get; set; }
        public bool is_primary { get; set; }
        public bool is_locked { get; set; }
        public string phone { get; set; }
        public string custom_value1 { get; set; }
        public string custom_value2 { get; set; }
        public string custom_value3 { get; set; }
        public string custom_value4 { get; set; }
        public string contact_key { get; set; }
        public bool send_email { get; set; }
        public int last_login { get; set; }
        public string password { get; set; }
        public string link { get; set; }
    }

    public class ClientGateway_Tokens
    {
        public string id { get; set; }
        public string token { get; set; }
        public string gateway_customer_reference { get; set; }
        public string gateway_type_id { get; set; }
        public string company_gateway_id { get; set; }
        public bool is_default { get; set; }
        public ClientMeta meta { get; set; }
        public int created_at { get; set; }
        public int updated_at { get; set; }
        public int archived_at { get; set; }
        public bool is_deleted { get; set; }
    }

    public class ClientMeta
    {
        public string exp_month { get; set; }
        public string exp_year { get; set; }
        public string brand { get; set; }
        public string last4 { get; set; }
        public int type { get; set; }
    }
}