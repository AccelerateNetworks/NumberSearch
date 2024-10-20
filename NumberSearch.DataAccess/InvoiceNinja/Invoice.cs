﻿using Flurl.Http;

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.InvoiceNinja
{
    public class Invoice
    {
        public InvoiceDatum[] data { get; set; } = Array.Empty<InvoiceDatum>();
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
                .GetJsonAsync<Invoice>()
                .ConfigureAwait(false);

            return result?.data ?? Array.Empty<InvoiceDatum>();
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
                .GetJsonAsync<Invoice>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result?.data ?? Array.Empty<InvoiceDatum>();
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
            return result?.data ?? new();
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
                .GetJsonAsync<InvoiceSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result?.data ?? new();
        }
    }

    public class InvoiceSingle
    {
        public InvoiceDatum data { get; set; } = new();
    }

    public class InvoiceMeta
    {
        public InovicePagination pagination { get; set; } = new();
    }

    public class InovicePagination
    {
        public int total { get; set; }
        public int count { get; set; }
        public int per_page { get; set; }
        public int current_page { get; set; }
        public int total_pages { get; set; }
        public InvoiceLinks links { get; set; } = new();
    }

    public class InvoiceLinks
    {
        public string next { get; set; } = string.Empty;
    }

    public class Line_Items
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal quantity { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal cost { get; set; }
        public string product_key { get; set; } = string.Empty;
        public string notes { get; set; } = string.Empty;
        public int discount { get; set; }
        public string tax_name1 { get; set; } = string.Empty;
        public int tax_rate1 { get; set; }
        public string tax_name2 { get; set; } = string.Empty;
        public int tax_rate2 { get; set; }
        public string tax_name3 { get; set; } = string.Empty;
        public int tax_rate3 { get; set; }
        public string date { get; set; } = string.Empty;
        public string custom_value1 { get; set; } = string.Empty;
        public string custom_value2 { get; set; } = string.Empty;
        public string custom_value3 { get; set; } = string.Empty;
        public string custom_value4 { get; set; } = string.Empty;
        public string type_id { get; set; } = string.Empty;
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal product_cost { get; set; }
        [JsonConverter(typeof(BooleanConverter))]
        public bool is_amount_discount { get; set; }
        public string sort_id { get; set; } = string.Empty;
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal line_total { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal gross_line_total { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal tax_amount { get; set; }
        public string tax_id { get; set; } = string.Empty;
    }


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

    public class Invitation
    {
        public string id { get; set; } = string.Empty;
        public string client_contact_id { get; set; } = string.Empty;
        public string key { get; set; } = string.Empty;
        public string link { get; set; } = string.Empty;
        public string sent_date { get; set; } = string.Empty;
        public string viewed_date { get; set; } = string.Empty;
        public string opened_date { get; set; } = string.Empty;
        public int updated_at { get; set; }
        public int archived_at { get; set; }
        public int created_at { get; set; }
        public string email_status { get; set; } = string.Empty;
        public string email_error { get; set; } = string.Empty;
    }

    public class InvoiceDatum
    {
        public string id { get; set; } = string.Empty;
        public string user_id { get; set; } = string.Empty;
        public string project_id { get; set; } = string.Empty;
        public string assigned_user_id { get; set; } = string.Empty;
        public decimal amount { get; set; }
        public decimal balance { get; set; }
        public string client_id { get; set; } = string.Empty;
        public string vendor_id { get; set; } = string.Empty;
        public string status_id { get; set; } = string.Empty;
        public string design_id { get; set; } = string.Empty;
        public string invoice_id { get; set; } = string.Empty;
        public string recurring_id { get; set; } = string.Empty;
        public int created_at { get; set; }
        public int updated_at { get; set; }
        public int archived_at { get; set; }
        public bool is_deleted { get; set; }
        public string number { get; set; } = string.Empty;
        public decimal discount { get; set; }
        public string po_number { get; set; } = string.Empty;
        public string date { get; set; } = string.Empty;
        public string last_sent_date { get; set; } = string.Empty;
        public string next_send_date { get; set; } = string.Empty;
        public string due_date { get; set; } = string.Empty;
        public string terms { get; set; } = string.Empty;
        public string public_notes { get; set; } = string.Empty;
        public string private_notes { get; set; } = string.Empty;
        public bool uses_inclusive_taxes { get; set; }
        public string tax_name1 { get; set; } = string.Empty;
        public decimal tax_rate1 { get; set; }
        public string tax_name2 { get; set; } = string.Empty;
        public int tax_rate2 { get; set; }
        public string tax_name3 { get; set; } = string.Empty;
        public int tax_rate3 { get; set; }
        public decimal total_taxes { get; set; }
        public bool is_amount_discount { get; set; }
        public string footer { get; set; } = string.Empty;
        public int partial { get; set; }
        public string partial_due_date { get; set; } = string.Empty;
        public string custom_value1 { get; set; } = string.Empty;
        public string custom_value2 { get; set; } = string.Empty;
        public string custom_value3 { get; set; } = string.Empty;
        public string custom_value4 { get; set; } = string.Empty;
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
        public Line_Items[] line_items { get; set; } = Array.Empty<Line_Items>();
        public string entity_type { get; set; } = string.Empty;
        public string reminder1_sent { get; set; } = string.Empty;
        public string reminder2_sent { get; set; } = string.Empty;
        public string reminder3_sent { get; set; } = string.Empty;
        public string reminder_last_sent { get; set; } = string.Empty;
        public float paid_to_date { get; set; }
        public string subscription_id { get; set; } = string.Empty;
        public bool auto_bill_enabled { get; set; }
        public Invitation[] invitations { get; set; } = Array.Empty<Invitation>();
        public object[] documents { get; set; } = Array.Empty<object>();

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
                .ReceiveJson<InvoiceSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result?.data ?? new();
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
                .ReceiveJson<InvoiceSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result?.data ?? new();
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
                .ReceiveJson<InvoiceSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result?.data ?? new();
        }
    }

    public class ReccurringInvoice
    {
        public ReccurringInvoiceDatum[] data { get; set; } = Array.Empty<ReccurringInvoiceDatum>();

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
                .GetJsonAsync<ReccurringInvoice>()
                .ConfigureAwait(false);

            return result?.data ?? Array.Empty<ReccurringInvoiceDatum>();
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
                .GetJsonAsync<ReccurringInvoiceSingle>()
                .ConfigureAwait(false);

            return result?.data ?? new();
        }
    }

    public class ReccurringInvoiceSingle
    {
        public ReccurringInvoiceDatum data { get; set; } = new();
    }

    public class ReccurringInvoiceDatum
    {
        public string id { get; set; } = string.Empty;
        public string user_id { get; set; } = string.Empty;
        public string project_id { get; set; } = string.Empty;
        public string assigned_user_id { get; set; } = string.Empty;
        public decimal amount { get; set; }
        public decimal balance { get; set; }
        public string client_id { get; set; } = string.Empty;
        public string vendor_id { get; set; } = string.Empty;
        public string status_id { get; set; } = string.Empty;
        public string design_id { get; set; } = string.Empty;
        public int created_at { get; set; }
        public int updated_at { get; set; }
        public int archived_at { get; set; }
        public bool is_deleted { get; set; }
        public string number { get; set; } = string.Empty;
        public decimal discount { get; set; }
        public string po_number { get; set; } = string.Empty;
        public string date { get; set; } = string.Empty;
        public string last_sent_date { get; set; } = string.Empty;
        public string next_send_date { get; set; } = string.Empty;
        public string due_date { get; set; } = string.Empty;
        public string terms { get; set; } = string.Empty;
        public string public_notes { get; set; } = string.Empty;
        public string private_notes { get; set; } = string.Empty;
        public bool uses_inclusive_taxes { get; set; }
        public string tax_name1 { get; set; } = string.Empty;
        public decimal tax_rate1 { get; set; }
        public string tax_name2 { get; set; } = string.Empty;
        public decimal tax_rate2 { get; set; }
        public string tax_name3 { get; set; } = string.Empty;
        public decimal tax_rate3 { get; set; }
        public decimal total_taxes { get; set; }
        public bool is_amount_discount { get; set; }
        public string footer { get; set; } = string.Empty;
        public decimal partial { get; set; }
        public string partial_due_date { get; set; } = string.Empty;
        public string custom_value1 { get; set; } = string.Empty;
        public string custom_value2 { get; set; } = string.Empty;
        public string custom_value3 { get; set; } = string.Empty;
        public string custom_value4 { get; set; } = string.Empty;
        public bool has_tasks { get; set; }
        public bool has_expenses { get; set; }
        public decimal custom_surcharge1 { get; set; }
        public decimal custom_surcharge2 { get; set; }
        public decimal custom_surcharge3 { get; set; }
        public decimal custom_surcharge4 { get; set; }
        public decimal exchange_rate { get; set; }
        public bool custom_surcharge_tax1 { get; set; }
        public bool custom_surcharge_tax2 { get; set; }
        public bool custom_surcharge_tax3 { get; set; }
        public bool custom_surcharge_tax4 { get; set; }
        public Line_Items[] line_items { get; set; } = Array.Empty<Line_Items>();
        public string entity_type { get; set; } = string.Empty;
        public string frequency_id { get; set; } = string.Empty;
        public int remaining_cycles { get; set; }
        public object[] recurring_dates { get; set; } = Array.Empty<object>();
        public string auto_bill { get; set; } = string.Empty;
        public bool auto_bill_enabled { get; set; }
        public string due_date_days { get; set; } = string.Empty;
        public decimal paid_to_date { get; set; }
        public string subscription_id { get; set; } = string.Empty;
        public Invitation[] invitations { get; set; } = Array.Empty<Invitation>();
        public object[] documents { get; set; } = Array.Empty<object>();

        public async Task<ReccurringInvoiceDatum> PostAsync(string token)
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
                .WithHeader(tokenHeader, token)
                .WithHeader(requestedHeader, requestedHeaderValue)
                .WithHeader(contentHeader, contentHeaderValue)
                .PostJsonAsync(new { client_id, tax_name1, tax_rate1, entity_type, frequency_id, auto_bill_enabled, auto_bill, line_items })
                .ReceiveJson<ReccurringInvoiceSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result?.data ?? new();
        }

        public async Task<ReccurringInvoiceDatum> PutAsync(string token)
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
                .WithHeader(tokenHeader, token)
                .WithHeader(requestedHeader, requestedHeaderValue)
                .WithHeader(contentHeader, contentHeaderValue)
                .PutJsonAsync(new { client_id, tax_name1, tax_rate1, entity_type, frequency_id, auto_bill_enabled, auto_bill, line_items })
                .ReceiveJson<ReccurringInvoiceSingle>()
                .ConfigureAwait(false);

            // Unwrap the data we want from the single-field parent object.
            return result?.data ?? new();
        }
    }
}
