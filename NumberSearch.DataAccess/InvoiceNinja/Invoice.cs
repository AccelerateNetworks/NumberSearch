using Flurl.Http;

using System.Threading.Tasks;

namespace NumberSearch.DataAccess.InvoiceNinja
{
    public class Invoice
    {
        public InvoiceDatum[] data { get; set; }
        //public InvoiceMeta meta { get; set; }

        public static async Task<InvoiceDatum[]> GetAllAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpostring = "invoices";
            string tokenHeader = "X-Api-Token";
            string perPageParameter = "?per_page=10000";
            string url = $"{baseUrl}{endpostring}{perPageParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<Invoice>()
                .ConfigureAwait(false);

            return result.data;
        }

        public static async Task<InvoiceDatum[]> GetByClientIdWithInoviceLinksAsync(string clientId, string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "invoices";
            string tokenHeader = "X-Api-Token";
            string clientIdParameter = $"?client_id={clientId}";
            string perPageParameter = "&per_page=10000";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}{perPageParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<Invoice>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public static async Task<InvoiceDatum> GetByIdAsync(string invoiceId, string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpostring = "invoices";
            string tokenHeader = "X-Api-Token";
            string clientIdParameter = $"/{invoiceId}";
            string url = $"{baseUrl}{endpostring}{clientIdParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<InvoiceSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }


    }

    public class InvoiceSingle
    {
        public InvoiceDatum data { get; set; }
    }

    public class InvoiceMeta
    {
        public InovicePagination pagination { get; set; }
    }

    public class InovicePagination
    {
        public int total { get; set; }
        public int count { get; set; }
        public int per_page { get; set; }
        public int current_page { get; set; }
        public int total_pages { get; set; }
        public InvoiceLinks links { get; set; }
    }

    public class InvoiceLinks
    {
        public string next { get; set; }
    }

    public class Line_Items
    {
        public decimal quantity { get; set; }
        public decimal cost { get; set; }
        public string product_key { get; set; }
        public string notes { get; set; }
        public int discount { get; set; }
        public string tax_name1 { get; set; }
        public int tax_rate1 { get; set; }
        public string tax_name2 { get; set; }
        public int tax_rate2 { get; set; }
        public string tax_name3 { get; set; }
        public int tax_rate3 { get; set; }
        public string date { get; set; }
        public string custom_value1 { get; set; }
        public string custom_value2 { get; set; }
        public string custom_value3 { get; set; }
        public string custom_value4 { get; set; }
        public string type_id { get; set; }
        public decimal product_cost { get; set; }
        public bool is_amount_discount { get; set; }
        public string sort_id { get; set; }
        public decimal line_total { get; set; }
        public decimal gross_line_total { get; set; }
        public decimal tax_amount { get; set; }
    }

    public class Invitation
    {
        public string id { get; set; }
        public string client_contact_id { get; set; }
        public string key { get; set; }
        public string link { get; set; }
        public string sent_date { get; set; }
        public string viewed_date { get; set; }
        public string opened_date { get; set; }
        public int updated_at { get; set; }
        public int archived_at { get; set; }
        public int created_at { get; set; }
        public string email_status { get; set; }
        public string email_error { get; set; }
    }


    public class InvoiceDatum
    {
        public string id { get; set; }
        public string user_id { get; set; }
        public string project_id { get; set; }
        public string assigned_user_id { get; set; }
        public decimal amount { get; set; }
        public decimal balance { get; set; }
        public string client_id { get; set; }
        public string vendor_id { get; set; }
        public string status_id { get; set; }
        public string design_id { get; set; }
        public string recurring_id { get; set; }
        public int created_at { get; set; }
        public int updated_at { get; set; }
        public int archived_at { get; set; }
        public bool is_deleted { get; set; }
        public string number { get; set; }
        public decimal discount { get; set; }
        public string po_number { get; set; }
        public string date { get; set; }
        public string last_sent_date { get; set; }
        public string next_send_date { get; set; }
        public string due_date { get; set; }
        public string terms { get; set; }
        public string public_notes { get; set; }
        public string private_notes { get; set; }
        public bool uses_inclusive_taxes { get; set; }
        public string tax_name1 { get; set; }
        public decimal tax_rate1 { get; set; }
        public string tax_name2 { get; set; }
        public int tax_rate2 { get; set; }
        public string tax_name3 { get; set; }
        public int tax_rate3 { get; set; }
        public decimal total_taxes { get; set; }
        public bool is_amount_discount { get; set; }
        public string footer { get; set; }
        public int partial { get; set; }
        public string partial_due_date { get; set; }
        public string custom_value1 { get; set; }
        public string custom_value2 { get; set; }
        public string custom_value3 { get; set; }
        public string custom_value4 { get; set; }
        public bool has_tasks { get; set; }
        public bool has_expenses { get; set; }
        public int custom_surcharge1 { get; set; }
        public int custom_surcharge2 { get; set; }
        public int custom_surcharge3 { get; set; }
        public int custom_surcharge4 { get; set; }
        public int exchange_rate { get; set; }
        public bool custom_surcharge_tax1 { get; set; }
        public bool custom_surcharge_tax2 { get; set; }
        public bool custom_surcharge_tax3 { get; set; }
        public bool custom_surcharge_tax4 { get; set; }
        public Line_Items[] line_items { get; set; }
        public string entity_type { get; set; }
        public string reminder1_sent { get; set; }
        public string reminder2_sent { get; set; }
        public string reminder3_sent { get; set; }
        public string reminder_last_sent { get; set; }
        public float paid_to_date { get; set; }
        public string subscription_id { get; set; }
        public bool auto_bill_enabled { get; set; }
        public Invitation[] invitations { get; set; }
        public object[] documents { get; set; }

        public async Task<InvoiceDatum> PostAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "invoices";
            string tokenHeader = "X-Api-Token";
            string requestedHeader = "X-Requested-With";
            string requestedHeaderValue = "XMLHttpRequest";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string url = $"{baseUrl}{endpoint}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .WithHeader(requestedHeader, requestedHeaderValue)
                .WithHeader(contentHeader, contentHeaderValue)
                .PostJsonAsync(new { client_id = id, tax_name1, tax_rate1, entity_type, line_items })
                .ReceiveJson<InvoiceSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public async Task<InvoiceDatum> PutAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "invoices";
            string tokenHeader = "X-Api-Token";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string clientIdParameter = $"/{id}";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .WithHeader(contentHeader, contentHeaderValue)
                .PutJsonAsync(new { id, line_items })
                .ReceiveJson<InvoiceSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public async Task<InvoiceDatum> DeleteAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "invoices";
            string tokenHeader = "X-Api-Token";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string clientIdParameter = $"/{id}";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}?action=delete";

            var result = await url
                .WithHeader(tokenHeader, token)
                .WithHeader(contentHeader, contentHeaderValue)
                .PutAsync()
                .ReceiveJson<InvoiceSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }
    }
}