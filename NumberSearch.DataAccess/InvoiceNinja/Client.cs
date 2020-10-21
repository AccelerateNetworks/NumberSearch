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
            string tokenHeader = "X-Ninja-Token";
            string url = $"{baseUrl}{endpoint}";

            return await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<Client>()
                .ConfigureAwait(false);
        }

        public static async Task<Client> GetByEmailAsync(string email, string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "clients";
            string tokenHeader = "X-Ninja-Token";
            string emailParameter = $"?email={email}";
            string url = $"{baseUrl}{endpoint}{emailParameter}";

            return await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<Client>()
                .ConfigureAwait(false);
        }

        public static async Task<Client> GetByClientIdAsync(string clientId, string token)
        {
            // This doesn't work right, rather it just returns the full list of clients.
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "clients";
            string tokenHeader = "X-Ninja-Token";
            string clientIdParameter = $"?client_id={clientId}";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}";

            return await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<Client>()
                .ConfigureAwait(false);
        }

        public static async Task<ClientDatum> GetByIdAsync(int clientId, string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "clients";
            string tokenHeader = "X-Ninja-Token";
            string clientIdParameter = $"/{clientId}";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<ClientSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public static async Task<ClientDatum> GetByIdWithInoviceLinksAsync(int clientId, string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "clients";
            string tokenHeader = "X-Ninja-Token";
            string clientIdParameter = $"/{clientId}";
            string invitationsParameter = $"?include=invoices.invitations";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}{invitationsParameter}";

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

    public class ClientMeta
    {
        public ClientPagination pagination { get; set; }
    }

    public class ClientPagination
    {
        public int total { get; set; }
        public int count { get; set; }
        public int per_page { get; set; }
        public int current_page { get; set; }
        public int total_pages { get; set; }

        // This field is sometimes an array and other times its just an object. This change in type break the JSON serializer.
        public ClientLinks links { get; set; }
    }


    public class ClientLinks
    {
        public string next { get; set; }
    }

    public class ClientDatum
    {
        public string account_key { get; set; }
        public bool is_owner { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public string display_name { get; set; }
        public float balance { get; set; }
        public float paid_to_date { get; set; }
        public int updated_at { get; set; }
        public object archived_at { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string postal_code { get; set; }
        public int country_id { get; set; }
        public string work_phone { get; set; }
        public string private_notes { get; set; }
        public string public_notes { get; set; }
        public string last_login { get; set; }
        public string website { get; set; }
        public int industry_id { get; set; }
        public int size_id { get; set; }
        public bool is_deleted { get; set; }
        public int payment_terms { get; set; }
        public string vat_number { get; set; }
        public string id_number { get; set; }
        public int language_id { get; set; }
        public int currency_id { get; set; }
        public string custom_value1 { get; set; }
        public string custom_value2 { get; set; }
        public int invoice_number_counter { get; set; }
        public int quote_number_counter { get; set; }
        public decimal task_rate { get; set; }
        public string shipping_address1 { get; set; }
        public string shipping_address2 { get; set; }
        public string shipping_city { get; set; }
        public string shipping_state { get; set; }
        public string shipping_postal_code { get; set; }
        public int shipping_country_id { get; set; }
        public bool show_tasks_in_portal { get; set; }
        public bool send_reminders { get; set; }
        public int credit_number_counter { get; set; }
        public string custom_messages { get; set; }
        public ClientContact[] contacts { get; set; }
        public InvoiceDatum[] invoices { get; set; }

        public async Task<ClientDatum> PostAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "clients";
            string tokenHeader = "X-Ninja-Token";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string url = $"{baseUrl}{endpoint}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .WithHeader(contentHeader, contentHeaderValue)
                .PostJsonAsync(new { name = name, contact = new { email = contacts.FirstOrDefault().email } })
                .ReceiveJson<ClientSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public async Task<ClientDatum> PutAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "clients";
            string tokenHeader = "X-Ninja-Token";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string clientIdParameter = $"/{id}";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .WithHeader(contentHeader, contentHeaderValue)
                .PutJsonAsync(new { name, contacts })
                .ReceiveJson<ClientSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public async Task<ClientDatum> DeleteAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "clients";
            string tokenHeader = "X-Ninja-Token";
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

    public class ClientContact
    {
        public string account_key { get; set; }
        public bool is_owner { get; set; }
        public int id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string contact_key { get; set; }
        public int updated_at { get; set; }
        public object archived_at { get; set; }
        public bool is_primary { get; set; }
        public string phone { get; set; }
        public string last_login { get; set; }
        public bool send_invoice { get; set; }
        public string custom_value1 { get; set; }
        public string custom_value2 { get; set; }
    }
}