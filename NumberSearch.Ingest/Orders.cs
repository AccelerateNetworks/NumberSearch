using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.InvoiceNinja;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class Orders
    {
        public async static Task UpdateOrdersAsync(IConfiguration configuration)
        {
            Log.Information("[OrdersUpdate] Ingesting data from the billing system.");

            var postgresSQL = configuration.GetConnectionString("PostgresqlProd");
            var invoiceNinjaToken = configuration.GetConnectionString("InvoiceNinjaToken");

            // Look for order updates in the billing system.
            try
            {
                // Look for quotes that have been converted to invoices.
                await UpdateQuoteStatusAsync(invoiceNinjaToken, postgresSQL);
            }
            catch (Exception ex)
            {
                Log.Fatal("[OrdersUpdate] Failed to look find updates for order in the billing system.");
                Log.Fatal(ex.Message);
                Log.Fatal(ex.StackTrace);
            }

            Log.Information("[OrdersUpdate] Completed the ingest of data from the billing system.");
        }

        public class OrderStatus
        {
            public Guid OrderId { get; set; }
            public string Status { get; set; }
            public string Customer { get; set; }
        }

        public static async Task<IEnumerable<OrderStatus>> OrdersRequiringPortingInformationAsync(string connectionString)
        {

            var orders = await Order.GetAllAsync(connectionString);
            var portRequests = await PortRequest.GetAllAsync(connectionString);

            var orderStatuses = new List<OrderStatus>();

            foreach (var order in orders)
            {
                var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, connectionString);
                var productOrders = await ProductOrder.GetAsync(order.OrderId, connectionString);
                var businessName = string.IsNullOrWhiteSpace(order.BusinessName) ? "Consumer" : order.BusinessName;
                var nextStep = "Next Step";
                var pillColor = "danger";
                // The order is completed, we're good.
                if (order?.Completed is true)
                {
                    nextStep = "🎉 Done, Good Job";
                    pillColor = "success";
                }
                // The order is not completed, and is stale.
                else if (order?.Completed is not true && order?.DateSubmitted <
                    DateTime.Now.AddDays(-14))
                {
                    nextStep = "⭕ Contact the Customer, the order is Stale";
                    pillColor = "warning";
                }
                else if (order?.Completed is not true)
                {
                    if (order.Quote)
                    {
                        nextStep = $"⭕ Pending Quote Approval";
                        pillColor = "warning";
                    }
                    else if (portRequest is null && productOrders.Where(x => x.PortedPhoneNumberId.HasValue is true).Any())
                    {
                        nextStep = $"⭕ Get the Porting information from the Customer";
                        pillColor = "danger";
                        orderStatuses.Add(new OrderStatus { OrderId = order.OrderId, Status = nextStep, Customer = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order?.FirstName} {order?.LastName}" : order.BusinessName });
                    }
                    else if (portRequest is not null)
                    {
                        if (portRequest?.Completed is true)
                        {
                            if (order?.OnsiteInstallation is true)
                            {
                                nextStep = $"⭕ Install the cusomter's hardware onsite {order?.InstallDate.GetValueOrDefault().ToShortDateString()}";
                                pillColor = "info";
                                orderStatuses.Add(new OrderStatus { OrderId = order.OrderId, Status = nextStep, Customer = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order?.FirstName} {order?.LastName}" : order.BusinessName });
                            }
                            else
                            {
                                nextStep = $"⭕ Ship the hardware to the customer for self-install";
                                pillColor = "info";
                                orderStatuses.Add(new OrderStatus { OrderId = order.OrderId, Status = nextStep, Customer = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order?.FirstName} {order?.LastName}" : order.BusinessName });
                            }
                        }
                        else
                        {
                            nextStep = "⭕ Port the Customer's Numbers to our Network";
                            pillColor = "danger";
                            orderStatuses.Add(new OrderStatus { OrderId = order.OrderId, Status = nextStep, Customer = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order?.FirstName} {order?.LastName}" : order.BusinessName });
                        }
                    }
                    else
                    {
                        if (order?.OnsiteInstallation is true)
                        {
                            nextStep = $"⭕ Install the cusomter's hardware onsite";
                            pillColor = "info";
                            orderStatuses.Add(new OrderStatus { OrderId = order.OrderId, Status = nextStep, Customer = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order?.FirstName} {order?.LastName}" : order.BusinessName });
                        }
                        else
                        {
                            nextStep = $"⭕ Ship the hardware to the customer for self-install";
                            pillColor = "info";
                            orderStatuses.Add(new OrderStatus { OrderId = order.OrderId, Status = nextStep, Customer = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order?.FirstName} {order?.LastName}" : order.BusinessName });
                        }
                    }
                }
            }

            return orderStatuses;
        }

        public static async Task<bool> SendOrderReminderEmailAsync(IEnumerable<OrderStatus> changes, string smtpUsername, string smtpPassword, string emailPrimary, string emailCC, string connectionString)
        {
            if ((changes is null) || !changes.Any())
            {
                // Successfully did nothing.
                return true;
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var output = new StringBuilder();

            output.Append(JsonSerializer.Serialize(changes, options));

            var notificationEmail = new Email
            {
                PrimaryEmailAddress = emailPrimary,
                CarbonCopy = emailCC,
                DateSent = DateTime.Now,
                Subject = $"[Ingest] {changes.Count()} active orders.",
                MessageBody = output.ToString(),
                OrderId = new Guid(),
                Completed = true
            };

            var checkSend = await notificationEmail.SendEmailAsync(smtpUsername, smtpPassword).ConfigureAwait(false);
            var checkSave = await notificationEmail.PostAsync(connectionString).ConfigureAwait(false);

            return checkSave && checkSend;
        }

        public static async Task UpdateQuoteStatusAsync(string invoiceNinjaToken, string connectionString)
        {
            var orders = await Order.GetAllAsync(connectionString);

            foreach (var order in orders)
            {
                // There are invoices associated with the order verify them.
                if (!string.IsNullOrWhiteSpace(order.BillingInvoiceId) || !string.IsNullOrWhiteSpace(order.BillingInvoiceReoccuringId))
                {
                    var orderUpdated = false;
                    Log.Information($"[OrdersUpdate] Verifying Order {order.OrderId} {nameof(order.BillingInvoiceId)} {order.BillingInvoiceId} {nameof(order.BillingInvoiceReoccuringId)} {order.BillingInvoiceReoccuringId}");

                    // Handle the upfront invoice
                    if (int.TryParse(order.BillingInvoiceId, out int upfrontId))
                    {
                        try
                        {
                            var upfrontInvoice = await Invoice.GetByIdAsync(upfrontId, invoiceNinjaToken);
                            // If we found a matching invoice in the billing system and the quote has been upgraded to an invoice.
                            if (upfrontInvoice is not null && upfrontInvoice.quote_invoice_id > 0 && upfrontId != upfrontInvoice.quote_invoice_id)
                            {
                                // Update the order with the new invoiceId.
                                Log.Information($"[OrderUpdates] Updated Order {order.OrderId} from {nameof(order.BillingInvoiceId)} {order.BillingInvoiceId} to {upfrontInvoice.quote_invoice_id}.");
                                order.BillingInvoiceId = upfrontInvoice.quote_invoice_id.ToString();
                                order.BillingClientId = upfrontInvoice.client_id.ToString();
                                orderUpdated = true;
                            }
                        }
                        catch
                        {
                            Log.Error($"[OrderUpdates] Failed to find invoice {order.BillingInvoiceId} in the billing system.");
                        }
                    }

                    // Handle the reoccuring invoice.
                    if (int.TryParse(order.BillingInvoiceReoccuringId, out int reoccuringId))
                    {
                        try
                        {
                            var reoccuringInvoice = await Invoice.GetByIdAsync(reoccuringId, invoiceNinjaToken);
                            // If we found a matching invoice in the billing system and the quote has been upgraded to an invoice.
                            if (reoccuringInvoice is not null && reoccuringInvoice.quote_invoice_id > 0 && reoccuringId != reoccuringInvoice.quote_invoice_id)
                            {
                                // Update the order with the new invoiceId.
                                Log.Information($"[OrderUpdates] Updated Order {order.OrderId} from {nameof(order.BillingInvoiceReoccuringId)} {order.BillingInvoiceReoccuringId} to {reoccuringInvoice.quote_invoice_id}.");
                                order.BillingInvoiceReoccuringId = reoccuringInvoice.quote_invoice_id.ToString();
                                order.BillingClientId = reoccuringInvoice.client_id.ToString();
                                orderUpdated = true;
                            }
                        }
                        catch
                        {
                            Log.Error($"[OrderUpdates] Failed to find invoice {order.BillingInvoiceReoccuringId} in the billing system.");
                        }
                    }

                    // If the billing invoices have been updated (converted from quotes to invoices) then the order is no longer a quote.
                    if (order.Quote && orderUpdated)
                    {
                        Log.Information($"[OrderUpdates] Order {order.OrderId} upgraded from a Quote.");

                        // Update the invoice links shown on the order.
                        if (int.TryParse(order.BillingClientId, out int clientId))
                        {
                            var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(clientId, invoiceNinjaToken).ConfigureAwait(false);

                            if (int.TryParse(order.BillingInvoiceId, out upfrontId))
                            {
                                var oneTimeLink = invoiceLinks.invoices.Where(x => x.id == upfrontId).FirstOrDefault()?.invitations.FirstOrDefault()?.link;
                                if (!string.IsNullOrWhiteSpace(oneTimeLink))
                                {
                                    order.UpfrontInvoiceLink = oneTimeLink;
                                }
                            }

                            if (int.TryParse(order.BillingInvoiceReoccuringId, out reoccuringId))
                            {
                                var reoccuringLink = invoiceLinks.invoices.Where(x => x.id == reoccuringId).FirstOrDefault()?.invitations.FirstOrDefault()?.link;
                                if (!string.IsNullOrWhiteSpace(reoccuringLink))
                                {
                                    order.ReoccuringInvoiceLink = reoccuringLink;
                                }
                            }

                            Log.Information($"[OrderUpdates] Order {order.OrderId} updated Invoice links.");
                        }

                        order.Quote = false;
                    }

                    // Submit the updates to the db if there are any.
                    if (orderUpdated)
                    {
                        if (await order.PutAsync(connectionString))
                        {
                            Log.Information($"[OrderUpdates] Updated Order {order.OrderId}");
                        }
                        else
                        {
                            Log.Error($"[OrderUpdates] Failed to update Order {order.OrderId}");

                        }
                    }
                }
            }
        }
    }
}
