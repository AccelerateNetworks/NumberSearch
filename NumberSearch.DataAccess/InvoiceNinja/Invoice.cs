using Flurl.Http;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.InvoiceNinja
{
    public readonly record struct Invoice(InvoiceDatum[] data)
    {
        //public InvoiceMeta meta { get; set; }

        public static async Task<InvoiceDatum[]> GetAllAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpostring = "invoices";
            string tokenHeader = "X-Api-Token";
            string perPageParameter = "?per_page=10000";
            string url = $"{baseUrl}{endpostring}{perPageParameter}";

            var x = await url
                .WithHeader(tokenHeader, token)
                .GetStringAsync();

            var result = await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<Invoice>();

            return result.data;
        }

        public static async Task<InvoiceDatum[]> GetByClientIdWithInoviceLinksAsync(string clientId, string token, bool quote)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "invoices";
            if (quote)
            {
                endpoint = "quotes";
            }
            string tokenHeader = "X-Api-Token";
            string clientIdParameter = $"?client_id={clientId}";
            string perPageParameter = "&per_page=10000";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}{perPageParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<Invoice>();

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
                .GetJsonAsync<InvoiceSingle>();

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public static async Task<InvoiceDatum> GetQuoteByIdAsync(string invoiceId, string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpostring = "quotes";
            string tokenHeader = "X-Api-Token";
            string clientIdParameter = $"/{invoiceId}";
            string url = $"{baseUrl}{endpostring}{clientIdParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<InvoiceSingle>();

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }
    }

    public readonly record struct InvoiceSingle(InvoiceDatum data);

    public readonly record struct InvoiceMeta(InovicePagination pagination);

    public readonly record struct InovicePagination
    (
        int total,
        int count,
        int per_page,
        int current_page,
        int total_pages,
        InvoiceLinks links
    );

    public readonly record struct InvoiceLinks(string next);

    public readonly record struct Line_Items
    (
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        decimal quantity,
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        decimal cost,
        string product_key,
        string notes,
        int discount,
        string tax_name1,
        int tax_rate1,
        string tax_name2,
        int tax_rate2,
        string tax_name3,
        int tax_rate3,
        string date,
        string custom_value1,
        string custom_value2,
        string custom_value3,
        string custom_value4,
        string type_id,
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        decimal product_cost,
        [property: JsonConverter(typeof(BooleanConverter))]
        bool is_amount_discount,
        string sort_id,
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        decimal line_total,
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        decimal gross_line_total,
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        decimal tax_amount,
        string tax_id
    );


    public class BooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.String:
                    return reader.GetString() switch
                    {
                        "true" => true,
                        "false" => false,
                        _ => false
                    };
                default:
                    return false;
            }
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }

    public readonly record struct Invitation
    (
        string id,
        string client_contact_id,
        string key,
        string link,
        string sent_date,
        string viewed_date,
        string opened_date,
        int updated_at,
        int archived_at,
        int created_at,
        string email_status,
        string email_error
    );

    public readonly record struct InvoiceDatum
    (
        string id,
        string user_id,
        string project_id,
        string assigned_user_id,
        decimal amount,
        decimal balance,
        string client_id,
        string vendor_id,
        string status_id,
        string design_id,
        string invoice_id,
        string recurring_id,
        int created_at,
        int updated_at,
        int archived_at,
        bool is_deleted,
        string number,
        decimal discount,
        string po_number,
        string date,
        string last_sent_date,
        string next_send_date,
        string due_date,
        string terms,
        string public_notes,
        string private_notes,
        bool uses_inclusive_taxes,
        string tax_name1,
        decimal tax_rate1,
        string tax_name2,
        int tax_rate2,
        string tax_name3,
        int tax_rate3,
        decimal total_taxes,
        bool is_amount_discount,
        string footer,
        int partial,
        string partial_due_date,
        string custom_value1,
        string custom_value2,
        string custom_value3,
        string custom_value4,
        bool has_tasks,
        bool has_expenses,
        int custom_surcharge1,
        int custom_surcharge2,
        int custom_surcharge3,
        int custom_surcharge4,
        int exchange_rate,
        bool custom_surcharge_tax1,
        bool custom_surcharge_tax2,
        bool custom_surcharge_tax3,
        bool custom_surcharge_tax4,
        Line_Items[] line_items,
        string entity_type,
        string reminder1_sent,
        string reminder2_sent,
        string reminder3_sent,
        string reminder_last_sent,
        float paid_to_date,
        string subscription_id,
        bool auto_bill_enabled,
        Invitation[] invitations,
        object[] documents
    )
    {
        public async Task<InvoiceDatum> PostAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "invoices";
            if (entity_type is not null && entity_type is "quote")
            {
                endpoint = "quotes";
            }
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
                .PostJsonAsync(new { client_id, tax_name1, tax_rate1, entity_type, line_items })
                .ReceiveJson<InvoiceSingle>();

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public async Task<InvoiceDatum> PutAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "invoices";
            if (entity_type is not null && entity_type is "quote")
            {
                endpoint = "quotes";
            }
            string tokenHeader = "X-Api-Token";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string clientIdParameter = $"/{id}";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .WithHeader(contentHeader, contentHeaderValue)
                .PutJsonAsync(new { id, line_items, tax_name1, tax_rate1 })
                .ReceiveJson<InvoiceSingle>();

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public async Task<InvoiceDatum> DeleteAsync(string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "invoices";
            if (entity_type is not null && entity_type is "quote")
            {
                endpoint = "quotes";
            }
            string tokenHeader = "X-Api-Token";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string IdParameter = $"/{id}";
            string url = $"{baseUrl}{endpoint}{IdParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .WithHeader(contentHeader, contentHeaderValue)
                .DeleteAsync()
                .ReceiveJson<InvoiceSingle>();

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }
    }

    public readonly record struct ReccurringInvoice(ReccurringInvoiceDatum[] data)
    {
        public static async Task<ReccurringInvoiceDatum[]> GetByClientIdWithLinksAsync(string clientId, string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "recurring_invoices";
            string tokenHeader = "X-Api-Token";
            string clientIdParameter = $"?client_id={clientId}";
            string perPageParameter = "&per_page=10000";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}{perPageParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<ReccurringInvoice>();

            return result.data;
        }

        public static async Task<ReccurringInvoiceDatum> GetByIdAsync(string id, string token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "recurring_invoices";
            string tokenHeader = "X-Api-Token";
            string IdParameter = $"/{id}";
            string url = $"{baseUrl}{endpoint}{IdParameter}";

            var result = await url
                .WithHeader(tokenHeader, token)
                .GetJsonAsync<ReccurringInvoiceSingle>();

            return result.data;
        }
    }

    public readonly record struct ReccurringInvoiceSingle(ReccurringInvoiceDatum data);

    public readonly record struct ReccurringInvoiceDatum
    (
        string id,
        string user_id,
        string project_id,
        string assigned_user_id,
        decimal amount,
        decimal balance,
        string client_id,
        string vendor_id,
        string status_id,
        string design_id,
        int created_at,
        int updated_at,
        int archived_at,
        bool is_deleted,
        string number,
        decimal discount,
        string po_number,
        string date,
        string last_sent_date,
        string next_send_date,
        string due_date,
        string terms,
        string public_notes,
        string private_notes,
        bool uses_inclusive_taxes,
        string tax_name1,
        decimal tax_rate1,
        string tax_name2,
        decimal tax_rate2,
        string tax_name3,
        decimal tax_rate3,
        decimal total_taxes,
        bool is_amount_discount,
        string footer,
        decimal partial,
        string partial_due_date,
        string custom_value1,
        string custom_value2,
        string custom_value3,
        string custom_value4,
        bool has_tasks,
        bool has_expenses,
        decimal custom_surcharge1,
        decimal custom_surcharge2,
        decimal custom_surcharge3,
        decimal custom_surcharge4,
        decimal exchange_rate,
        bool custom_surcharge_tax1,
        bool custom_surcharge_tax2,
        bool custom_surcharge_tax3,
        bool custom_surcharge_tax4,
        Line_Items[] line_items,
        string entity_type,
        string frequency_id,
        int remaining_cycles,
        object[] recurring_dates,
        string auto_bill,
        bool auto_bill_enabled,
        string due_date_days,
        decimal paid_to_date,
        string subscription_id,
        Invitation[] invitations,
        object[] documents
    )
    {
        public async Task<ReccurringInvoiceDatum> PostAsync(ReadOnlyMemory<char> token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "recurring_invoices";
            string tokenHeader = "X-Api-Token";
            string requestedHeader = "X-Requested-With";
            string requestedHeaderValue = "XMLHttpRequest";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string url = $"{baseUrl}{endpoint}";

            var result = await url
                .WithHeader(tokenHeader, token.ToString())
                .WithHeader(requestedHeader, requestedHeaderValue)
                .WithHeader(contentHeader, contentHeaderValue)
                .PostJsonAsync(new { client_id, tax_name1, tax_rate1, entity_type, frequency_id, auto_bill_enabled, auto_bill, line_items })
                .ReceiveJson<ReccurringInvoiceSingle>();

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }

        public async Task<ReccurringInvoiceDatum> PutAsync(ReadOnlyMemory<char> token)
        {
            string baseUrl = "https://billing.acceleratenetworks.com/api/v1/";
            string endpoint = "recurring_invoices";
            string tokenHeader = "X-Api-Token";
            string requestedHeader = "X-Requested-With";
            string requestedHeaderValue = "XMLHttpRequest";
            string contentHeader = "Content-Type";
            string contentHeaderValue = "application/json";
            string clientIdParameter = $"/{id}";
            string url = $"{baseUrl}{endpoint}{clientIdParameter}";

            var result = await url
                .WithHeader(tokenHeader, token.ToString())
                .WithHeader(requestedHeader, requestedHeaderValue)
                .WithHeader(contentHeader, contentHeaderValue)
                .PutJsonAsync(new { client_id, tax_name1, tax_rate1, entity_type, frequency_id, auto_bill_enabled, auto_bill, line_items })
                .ReceiveJson<ReccurringInvoiceSingle>();

            // Unwrap the data we want from the single-field parent object.
            return result.data;
        }
    }
}
