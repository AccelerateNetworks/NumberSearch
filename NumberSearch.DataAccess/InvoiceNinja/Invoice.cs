using Flurl.Http;

using Serilog;

using System.Threading.Tasks;

namespace NumberSearch.DataAccess.InvoiceNinja
{
    public class Invoice
    {
        public InvoiceDatum[] data { get; set; }
        //public InvoiceMeta meta { get; set; }

        public static async Task<Invoice> GetAllAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "invoices";
            string tokenHeader = "X-Ninja-Token";
            string url = $"{baseUrl}{endpoint}";

            return await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<Invoice>()
                .ConfigureAwait(false);
        }

        public static async Task<InvoiceDatum> GetByIdAsync(int invoiceId, string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "invoices";
            string tokenHeader = "X-Ninja-Token";
            string clientIdParameter = $"/{invoiceId}";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}";

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

    public class InvoiceDatum
    {
        public string account_key { get; set; }
        public bool is_owner { get; set; }
        public int id { get; set; }
        public float amount { get; set; }
        public float balance { get; set; }
        public int client_id { get; set; }
        public int invoice_status_id { get; set; }
        public int updated_at { get; set; }
        public object archived_at { get; set; }
        public string invoice_number { get; set; }
        public int discount { get; set; }
        public string po_number { get; set; }
        public string invoice_date { get; set; }
        public string due_date { get; set; }
        public string terms { get; set; }
        public string public_notes { get; set; }
        public string private_notes { get; set; }
        public bool is_deleted { get; set; }
        public int invoice_type_id { get; set; }
        public bool is_recurring { get; set; }
        public int frequency_id { get; set; }
        public string start_date { get; set; }
        public string end_date { get; set; }
        public string last_sent_date { get; set; }
        public int recurring_invoice_id { get; set; }
        public string tax_name1 { get; set; }
        public float tax_rate1 { get; set; }
        public string tax_name2 { get; set; }
        public int tax_rate2 { get; set; }
        public bool is_amount_discount { get; set; }
        public string invoice_footer { get; set; }
        public int partial { get; set; }
        public string partial_due_date { get; set; }
        public bool has_tasks { get; set; }
        public bool auto_bill { get; set; }
        public int auto_bill_id { get; set; }
        public int custom_value1 { get; set; }
        public int custom_value2 { get; set; }
        public bool custom_taxes1 { get; set; }
        public bool custom_taxes2 { get; set; }
        public bool has_expenses { get; set; }
        public int quote_invoice_id { get; set; }
        public string custom_text_value1 { get; set; }
        public string custom_text_value2 { get; set; }
        public bool is_quote { get; set; }
        public bool is_public { get; set; }
        public string filename { get; set; }
        public int invoice_design_id { get; set; }
        public Invoice_Items[] invoice_items { get; set; }

        public async Task<InvoiceDatum> PostAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "invoices";
            string tokenHeader = "X-Ninja-Token";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string url = $"{baseUrl}{endpoint}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .WithHeader(contentHeader, contentHeaderValue)
                .PostJsonAsync(new { client_id = id, invoice_items })
                .ReceiveJson<InvoiceSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public async Task<InvoiceDatum> PutAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "invoices";
            string tokenHeader = "X-Ninja-Token";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string clientIdParameter = $"/{id}";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .WithHeader(contentHeader, contentHeaderValue)
                .PutJsonAsync(new { id, invoice_items })
                .ReceiveJson<InvoiceSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public async Task<InvoiceDatum> DeleteAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "invoices";
            string tokenHeader = "X-Ninja-Token";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string clientIdParameter = $"/{id}";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .WithHeader(contentHeader, contentHeaderValue)
                .DeleteAsync()
                .ReceiveJson<InvoiceSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public async Task<bool> SendInvoiceAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "email_invoice";
            string tokenHeader = "X-Ninja-Token";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string url = $"{baseUrl}{endpoint}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .WithHeader(contentHeader, contentHeaderValue)
                .PostJsonAsync(new { id })
                .ReceiveJson<Email_Invoice>()
                .ConfigureAwait(false);

            return result.message == "success";
        }
    }

    public class Invoice_Items
    {
        public string account_key { get; set; }
        public bool is_owner { get; set; }
        public int id { get; set; }
        public string product_key { get; set; }
        public int updated_at { get; set; }
        public object archived_at { get; set; }
        public string notes { get; set; }
        public float cost { get; set; }
        public decimal qty { get; set; }
        public string tax_name1 { get; set; }
        public int tax_rate1 { get; set; }
        public string tax_name2 { get; set; }
        public int tax_rate2 { get; set; }
        public int invoice_item_type_id { get; set; }
        public string custom_value1 { get; set; }
        public string custom_value2 { get; set; }
        public int discount { get; set; }
    }
}


public class Email_Invoice
{
    public string message { get; set; }
}
