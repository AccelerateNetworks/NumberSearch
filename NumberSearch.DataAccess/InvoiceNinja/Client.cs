using Flurl.Http;

using System;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.InvoiceNinja
{
    public readonly record struct Client(ClientDatum[] data)
    {
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
    public readonly record struct ClientSingle (ClientDatum data);

    public readonly record struct ClientDatum
    (
        string id,
        string user_id,
        string assigned_user_id,
        string name,
        string website,
        string private_notes,
        decimal balance,
        string group_settings_id,
        decimal paid_to_date,
        decimal credit_balance,
        int last_login,
        string size_id,
        string public_notes,
        string client_hash,
        string address1,
        string address2,
        string phone,
        string city,
        string state,
        string postal_code,
        string country_id,
        string industry_id,
        string custom_value1,
        string custom_value2,
        string custom_value3,
        string custom_value4,
        string shipping_address1,
        string shipping_address2,
        string shipping_city,
        string shipping_state,
        string shipping_postal_code,
        string shipping_country_id,
        ClientSettings settings,
        bool is_deleted,
        string vat_number,
        string id_number,
        int updated_at,
        int archived_at,
        int created_at,
        string display_name,
        string number,
        ClientContact[] contacts,
        object[] documents,
        ClientGateway_Tokens[] gateway_tokens,
        InvoiceDatum[] invoices
    ){

        public async Task<ClientDatum> PostAsync(ReadOnlyMemory<char> token)
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
                .ReceiveJson<ClientSingle>();

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public async Task<ClientDatum> PutAsync(ReadOnlyMemory<char> token)
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
                .ReceiveJson<ClientSingle>();

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public async Task<ClientDatum> DeleteAsync(ReadOnlyMemory<char> token)
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
                .ReceiveJson<ClientSingle>();

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }
    }

    public readonly record struct ClientSettings(string currency_id);

    public readonly record struct ClientContact
    (
        string id,
        string first_name,
        string last_name,
        string email,
        int created_at,
        int updated_at,
        int archived_at,
        bool is_primary,
        bool is_locked,
        string phone,
        string custom_value1,
        string custom_value2,
        string custom_value3,
        string custom_value4,
        string contact_key,
        bool send_email,
        int last_login,
        string password,
        string link
    );

    public readonly record struct ClientGateway_Tokens
    (
        string id,
        string token,
        string gateway_customer_reference,
        string gateway_type_id,
        string company_gateway_id,
        bool is_default,
        ClientMeta meta,
        int created_at,
        int updated_at,
        int archived_at,
        bool is_deleted
    );

    public readonly record struct ClientMeta
    (
        string exp_month,
        string exp_year,
        string brand,
        string last4,
        int type
    );
}