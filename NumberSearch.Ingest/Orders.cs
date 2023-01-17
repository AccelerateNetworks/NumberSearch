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
                if (!string.IsNullOrWhiteSpace(invoiceNinjaToken) && !string.IsNullOrWhiteSpace(postgresSQL))
                {
                    // Look for quotes that have been converted to invoices.
                    await UpdateQuoteStatusAsync(invoiceNinjaToken, postgresSQL);
                }
                else
                {
                    throw new Exception("The invoice ninja credentials are null.");
                }
            }
            catch (Exception ex)
            {
                Log.Fatal("[OrdersUpdate] Failed to look find updates for order in the billing system.");
                Log.Fatal(ex.Message);
                Log.Fatal(ex?.StackTrace ?? "No stacktrace found.");
            }

            Log.Information("[OrdersUpdate] Completed the ingest of data from the billing system.");
        }

        public class OrderStatus
        {
            public Guid OrderId { get; set; }
            public string? Status { get; set; }
            public string? Customer { get; set; }
        }

        public static async Task<IEnumerable<OrderStatus>> IncompleteOrderRemindersAsync(string connectionString)
        {
            var orders = await Order.GetAllAsync(connectionString);
            var portRequests = await PortRequest.GetAllAsync(connectionString);
            var orderStatuses = new List<OrderStatus>();

            foreach (var order in orders.OrderByDescending(x => x.DateSubmitted))
            {
                var portRequest = portRequests.Where(x => x.OrderId == order.OrderId).FirstOrDefault();

                var productOrders = await ProductOrder.GetAsync(order.OrderId, connectionString);

                var nextStep = Order.GetStatus(order, productOrders, portRequest);

                // If the order isn't complete and is not a quote add it to the reminders list.
                if (order.Completed is not true
                    && order.Quote is not true
                    && (portRequest is not null
                    || (productOrders is not null
                    && productOrders.Where(x => x.PortedPhoneNumberId.HasValue is true).Any())))
                {
                    orderStatuses.Add(new OrderStatus { OrderId = order.OrderId, Status = nextStep, Customer = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order?.FirstName} {order?.LastName}" : order.BusinessName });
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

            output.Append("These Orders need your attention and can be reviewed by clicking on them. 🤔\r");

            foreach (var item in changes)
            {
                output.Append($"\n\t<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{item.OrderId}</a>");
            }

            output.Append("\rThanks for looking into these, hombre! 🤠");

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

                    try
                    {
                        var upfrontInvoice = await Invoice.GetByIdAsync(order.BillingInvoiceId, invoiceNinjaToken);
                        // If we found a matching invoice in the billing system and the quote has been upgraded to an invoice.
                        if (upfrontInvoice is not null && !string.IsNullOrWhiteSpace(upfrontInvoice.id) && order.BillingInvoiceId != upfrontInvoice.id)
                        {
                            // Update the order with the new invoiceId.
                            Log.Information($"[OrderUpdates] Updated Order {order.OrderId} from {nameof(order.BillingInvoiceId)} {order.BillingInvoiceId} to {upfrontInvoice.id}.");
                            order.BillingInvoiceId = upfrontInvoice.id.ToString();
                            order.BillingClientId = upfrontInvoice.client_id.ToString();
                            orderUpdated = true;
                        }
                    }
                    catch
                    {
                        Log.Warning($"[OrderUpdates] Failed to find invoice {order.BillingInvoiceId} in the billing system.");
                    }


                    // Handle the reoccuring invoice.
                    try
                    {
                        var reoccuringInvoice = await Invoice.GetByIdAsync(order.BillingInvoiceReoccuringId, invoiceNinjaToken);
                        // If we found a matching invoice in the billing system and the quote has been upgraded to an invoice.
                        if (reoccuringInvoice is not null && !string.IsNullOrWhiteSpace(reoccuringInvoice.id) && order.BillingInvoiceReoccuringId != reoccuringInvoice.id)
                        {
                            // Update the order with the new invoiceId.
                            Log.Information($"[OrderUpdates] Updated Order {order.OrderId} from {nameof(order.BillingInvoiceReoccuringId)} {order.BillingInvoiceReoccuringId} to {reoccuringInvoice.id}.");
                            order.BillingInvoiceReoccuringId = reoccuringInvoice.id.ToString();
                            order.BillingClientId = reoccuringInvoice.client_id.ToString();
                            orderUpdated = true;
                        }
                    }
                    catch
                    {
                        Log.Warning($"[OrderUpdates] Failed to find invoice {order.BillingInvoiceReoccuringId} in the billing system.");
                    }


                    // If the billing invoices have been updated (converted from quotes to invoices) then the order is no longer a quote.
                    if (order.Quote && orderUpdated)
                    {
                        Log.Information($"[OrderUpdates] Order {order.OrderId} upgraded from a Quote.");

                        // Update the invoice links shown on the order.
                        var invoiceLinks = await Invoice.GetByClientIdWithInoviceLinksAsync(order.BillingClientId, invoiceNinjaToken, order.Quote).ConfigureAwait(false);

                        var oneTimeLink = invoiceLinks.Where(x => x.id.Contains(order.BillingInvoiceId)).FirstOrDefault()?.invitations.FirstOrDefault()?.link;
                        if (!string.IsNullOrWhiteSpace(oneTimeLink))
                        {
                            order.UpfrontInvoiceLink = oneTimeLink;
                        }

                        var reoccuringLink = invoiceLinks.Where(x => x.id.Contains(order.BillingInvoiceReoccuringId)).FirstOrDefault()?.invitations.FirstOrDefault()?.link;
                        if (!string.IsNullOrWhiteSpace(reoccuringLink))
                        {
                            order.ReoccuringInvoiceLink = reoccuringLink;
                        }

                        Log.Information($"[OrderUpdates] Order {order.OrderId} updated Invoice links.");

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
