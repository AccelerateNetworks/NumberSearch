using Flurl.Http;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.InvoiceNinja;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using static NumberSearch.Ingest.Program;

namespace NumberSearch.Ingest
{
    public class Orders
    {
        public static async Task<IngestStatistics> EmailDailyAsync(IngestConfiguration appConfig)
        {
            DateTime start = DateTime.Now;

            var checkBriefing = await Orders.DailyBriefingEmailAsync(appConfig);

            var combined = new IngestStatistics
            {
                StartDate = start,
                EndDate = DateTime.Now,
                FailedToIngest = 0,
                IngestedFrom = "DailyEmails",
                IngestedNew = 0,
                Lock = false,
                NumbersRetrived = 0,
                Removed = 0,
                Unchanged = 0,
                UpdatedExisting = 0,
                Priority = false
            };


            if (await combined.PostAsync(appConfig.Postgresql).ConfigureAwait(false))
            {
                Log.Information($"[DailyEmails] Sent out the emails {DateTime.Now}.");
            }
            else
            {
                Log.Fatal($"[DailyEmails] Failed to send out the emails {DateTime.Now}.");
            }

            return combined;
        }
        public static async Task<bool> DailyBriefingEmailAsync(IngestConfiguration appConfig)
        {
            // Gather all of the info to put into the daily email.
            var orders = await Order.GetAllAsync(appConfig.Postgresql);

            var ordersToMarkCompleted = new List<Order>();
            var ordersWithCompletedPortRequests = new List<Order>();
            var ordersWithUnfinishedPortRequests = new List<Order>();
            var ordersWithUnsubmittedPortRequests = new List<Order>();
            var ordersConvertedFromQuotesToday = new List<Order>();
            var ordersCompletedToday = new List<Order>();
            var ordersSubmittedToday = new List<Order>();
            var quotesSubmittedToday = new List<Order>();
            var oneWeekFollowUp = new List<Order>();
            var oneMonthFollowUp = new List<Order>();
            var yearlyFollowUp = new List<Order>();

            foreach (var order in orders)
            {
                // Orders that should be marked as complete because the install data has passed?
                if (order.Quote is false && order.InstallDate is not null && order.Completed is false && DateTime.Now > order.InstallDate && order.InstallDate.GetValueOrDefault().AddMonths(3) > DateTime.Now)
                {
                    ordersToMarkCompleted.Add(order);
                }
                // Orders that should be marked as complete now that the port requests are complete?
                else if (order.Quote is false && order.Completed is false)
                {
                    var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, appConfig.Postgresql);
                    if (portRequest is not null && portRequest?.State is "COMPLETE")
                    {
                        ordersWithCompletedPortRequests.Add(order);
                    }
                }
                // Orders that have unfinished port requests
                else if (order.Quote is false && order.Completed is false)
                {
                    var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, appConfig.Postgresql);
                    if (portRequest is not null && portRequest?.State is not "COMPLETE" && portRequest.DateSubmitted > order.DateSubmitted)
                    {
                        ordersWithUnfinishedPortRequests.Add(order);
                    }
                }
                // Orders that have unsubmitted port requests
                else if (order.Quote is false && order.Completed is false)
                {
                    var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, appConfig.Postgresql);
                    if (portRequest is not null && portRequest?.State is not "COMPLETE" && portRequest.DateSubmitted < order.DateSubmitted)
                    {
                        ordersWithUnsubmittedPortRequests.Add(order);
                    }
                }
                // Orders converted from quotes?
                else if (order.Quote is false && order.Completed is false && order.DateConvertedFromQuote >= DateTime.Now.AddDays(-2))
                {
                    ordersConvertedFromQuotesToday.Add(order);
                }
                // Orders completed today?
                else if (order.Quote is false && order.Completed is true && order.DateCompleted.HasValue is true && order.DateCompleted.Value >= DateTime.Now.AddDays(-2))
                {
                    ordersCompletedToday.Add(order);
                }
                else if (order.Quote is false && order.Completed is false && order.DateSubmitted >= DateTime.Now.AddDays(-2))
                {
                    ordersSubmittedToday.Add(order);
                }
                else if (order.Quote is true && order.DateSubmitted >= DateTime.Now.AddDays(-2))
                {
                    quotesSubmittedToday.Add(order);
                }
                else if (order.Quote is false && order.InstallDate is not null && order.InstallDate.Value.AddDays(7).Month == DateTime.Now.Month && order.InstallDate.Value.AddDays(7).Day == DateTime.Now.Day)
                {
                    oneWeekFollowUp.Add(order);
                }
                else if (order.Quote is false && order.InstallDate is not null && order.InstallDate.Value.AddMonths(1).Month == DateTime.Now.Month && order.InstallDate.Value.AddMonths(1).Day == DateTime.Now.Day)
                {
                    oneMonthFollowUp.Add(order);
                }
                else if (order.Quote is false && order.InstallDate is not null && order.InstallDate.Value.AddYears(1).DayOfYear == DateTime.Now.DayOfYear)
                {
                    yearlyFollowUp.Add(order);
                }
            }
            var output = new StringBuilder();

            output.Append("<p>Hey Dan,</p><p>Here's everything you need to know about the orders in the Accelerate Networks system.</p>");

            output.Append("<p>Orders completed today:</p><ul>");
            if (ordersCompletedToday.Count > 0)
            {
                foreach (var item in ordersCompletedToday)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "support@acceleratenetworks.com" : item.SalesEmail;
                    var completedDate = item?.DateCompleted is not null ? item?.DateCompleted.GetValueOrDefault().ToShortDateString() : "No completed date set";
                    output.Append($"<li><a href='https://acceleratenetworks.com/cart/order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> submitted on {item.DateSubmitted.ToShortDateString()} - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://acceleratenetworks.com/cart/order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - Completed on {completedDate}</li>");
                }
                output.Append("</ul>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            output.Append("<p>Converted from quotes today:</p><ul>");
            if (ordersConvertedFromQuotesToday.Count > 0)
            {
                foreach (var item in ordersConvertedFromQuotesToday)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
                }
                output.Append("</ul>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            output.Append("<p>Unsubmitted port requests:</p><ul>");
            if (ordersWithUnsubmittedPortRequests.Count > 0)
            {
                foreach (var item in ordersWithUnsubmittedPortRequests)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a></li>");
                }
                output.Append("</ul>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            output.Append("<p>Unfinished port requests:</p><ul>");
            if (ordersWithUnfinishedPortRequests.Count > 0)
            {
                foreach (var item in ordersWithUnfinishedPortRequests)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a></li>");
                }
                output.Append("</ul>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            output.Append("<p>Uncompleted orders with completed port requests:</p><ul>");
            if (ordersWithCompletedPortRequests.Count > 0)
            {
                foreach (var item in ordersWithCompletedPortRequests)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
                }
                output.Append("</ul>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            output.Append("<p>Uncompleted orders where the install date has passed in the last quarter:</p><ul>");
            if (ordersToMarkCompleted.Count > 0)
            {
                foreach (var item in ordersToMarkCompleted)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
                }
                output.Append("</ul>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            output.Append("<p>Orders submitted today:</p><ul>");
            if (ordersSubmittedToday.Count > 0)
            {
                foreach (var item in ordersSubmittedToday)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
                }
                output.Append("</ul>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            output.Append("<p>Quotes submitted today:</p><ul>");
            if (oneMonthFollowUp.Count > 0)
            {
                foreach (var item in oneMonthFollowUp)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
                }
                output.Append("</ul>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            output.Append("<p>Follow up with Installs from last week:</p><ul>");
            if (oneWeekFollowUp.Count > 0)
            {
                foreach (var item in oneWeekFollowUp)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
                }
                output.Append("</ul>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            output.Append("<p>Follow up with Installs from last month:</p><ul>");
            if (oneMonthFollowUp.Count > 0)
            {
                foreach (var item in oneMonthFollowUp)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
                }
                output.Append("</ul>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            output.Append("<p>Follow up with Installs from last year:</p><ul>");
            if (yearlyFollowUp.Count > 0)
            {
                foreach (var item in yearlyFollowUp)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
                }
                output.Append("</ul>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            output.Append("<p>Have a great day, hombre! 🤠</p>");

            var notificationEmail = new Email
            {
                PrimaryEmailAddress = appConfig.EmailDan,
                CarbonCopy = appConfig.EmailTom,
                SalesEmailAddress = "support@acceleratenetworks.com",
                DateSent = DateTime.Now,
                Subject = $"[Ingest] Daily Briefing for {DateTime.Now.ToShortDateString()}",
                MessageBody = output.ToString(),
                OrderId = new Guid(),
                Completed = false
            };

            var checkSend = await notificationEmail.SendEmailAsync(appConfig.SmtpUsername, appConfig.SmtpPassword).ConfigureAwait(false);
            var checkSave = await notificationEmail.PostAsync(appConfig.Postgresql).ConfigureAwait(false);

            return checkSend && checkSave;
        }

        public async static Task CheckForQuoteConversionsAsync(string postgresql, string invoiceNinjaToken, string emailUsername, string emailPassword)
        {
            Log.Information($"[Quote Conversion] Looking for quotes that were converted to invoices in the billing system.");

            var orders = await Order.GetAllQuotesAsync(postgresql).ConfigureAwait(false);

            // Don't both checking orders that are from before we upgraded to the current version of invoiceNinja.
            foreach (var order in orders.Where(x => x.DateSubmitted > DateTime.Parse("02/01/2023")))
            {
                // Get the quotes in invoice ninja and see if they've been converted
                try
                {
                    if (!string.IsNullOrWhiteSpace(order.BillingInvoiceId) && order.DateConvertedFromQuote is null)
                    {
                        try
                        {
                            var upfront = await Invoice.GetQuoteByIdAsync(order.BillingInvoiceId, invoiceNinjaToken);

                            if (upfront is not null && upfront.id == order.BillingInvoiceId && !string.IsNullOrWhiteSpace(upfront.invoice_id))
                            {
                                var convertedInvoice = await Invoice.GetByIdAsync(upfront.invoice_id, invoiceNinjaToken);

                                string newUpfrontLink = convertedInvoice.invitations.FirstOrDefault()?.link ?? string.Empty;

                                order.BillingInvoiceId = convertedInvoice.id;
                                order.BillingClientId = convertedInvoice.client_id;
                                order.UpfrontInvoiceLink = string.IsNullOrWhiteSpace(newUpfrontLink) ? order.UpfrontInvoiceLink : newUpfrontLink;
                                order.Quote = false;
                                order.DateConvertedFromQuote = DateTime.Now;

                                // If the converted invoice has been paid, mark it as paid.
                                if (convertedInvoice.status_id is "4")
                                {
                                    order.DateUpfrontInvoicePaid = DateTime.Now;
                                }

                                var invoiceStatus = convertedInvoice.status_id is "4" ? "paid" : "approved";
                                var checkUpdate = await order.PutAsync(postgresql);
                                string name = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName;
                                var message = new Email
                                {
                                    SalesEmailAddress = string.IsNullOrWhiteSpace(order.SalesEmail) ? string.Empty : order.SalesEmail,
                                    PrimaryEmailAddress = "support@acceleratenetworks.com",
                                    CarbonCopy = "thomas.ryan@outlook.com",
                                    Subject = $"Quote {upfront.number} has been {invoiceStatus} by {name}",
                                    OrderId = order.OrderId,
                                    MessageBody = $@"<p>Hi Sales Team,</p><p>The new invoice {convertedInvoice.number} <a href='{order.UpfrontInvoiceLink}' target='_blank'>can be viewed here.</a> The order <a href='https://ops.acceleratenetworks.com/Home/Order/{order.OrderId}' target='_blank'>can be edited here</a>, please follow up with the customer to set an install date.</p><p>Have a great day, hombre! 🤠</p>"
                                };

                                // Send the message the email server.
                                var checkSend = await message.SendEmailAsync(emailUsername, emailPassword).ConfigureAwait(false);

                                // If it didn't work try it again.
                                if (!checkSend)
                                {
                                    checkSend = await message.SendEmailAsync(emailUsername, emailPassword).ConfigureAwait(false);
                                }

                                // Mark it as sent.
                                message.DateSent = DateTime.Now;
                                message.DoNotSend = false;
                                message.Completed = checkSend;

                                // Update the database with the email's new status.
                                var checkSave = await message.PostAsync(postgresql).ConfigureAwait(false);

                                // Log the success or failure of the operation.
                                if (checkSend && checkSave)
                                {
                                    Log.Information($"[Quote Conversion] Successfully sent out email {message.EmailId} for order {order.OrderId}.");
                                }
                                else
                                {
                                    Log.Fatal($"[Quote Conversion] Failed to sent out the email {message.EmailId} for order {order.OrderId}.");
                                }
                            }
                        }
                        catch (FlurlHttpException ex)
                        {
                            if (ex.StatusCode is 404)
                            {
                                // Maybe it's an invoice rather than a Quote
                                var convertedInvoice = await Invoice.GetByIdAsync(order.BillingInvoiceId, invoiceNinjaToken);

                                if (convertedInvoice is not null && convertedInvoice.id == order.BillingInvoiceId && !string.IsNullOrWhiteSpace(convertedInvoice.id))
                                {
                                    if (convertedInvoice.status_id is "4")
                                    {
                                        // mark the upfront invoice as paid and update the link
                                        order.DateUpfrontInvoicePaid = DateTime.Now;
                                    }

                                    order.BillingClientId = convertedInvoice.client_id;
                                    order.BillingInvoiceId = convertedInvoice.id;
                                    if (order.Quote is true || order.DateConvertedFromQuote is null)
                                    {
                                        order.Quote = false;
                                        order.DateConvertedFromQuote = DateTime.Now;
                                    }
                                    string newUpfrontLink = convertedInvoice.invitations.FirstOrDefault()?.link ?? string.Empty;
                                    order.UpfrontInvoiceLink = string.IsNullOrWhiteSpace(newUpfrontLink) ? order.UpfrontInvoiceLink : newUpfrontLink;


                                    var checkUpdate = await order.PutAsync(postgresql);
                                    string name = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName;
                                    var invoiceStatus = convertedInvoice.status_id is "4" ? "paid" : "converted from a quote";
                                    var message = new Email
                                    {
                                        SalesEmailAddress = string.IsNullOrWhiteSpace(order.SalesEmail) ? string.Empty : order.SalesEmail,
                                        PrimaryEmailAddress = "support@acceleratenetworks.com",
                                        CarbonCopy = "thomas.ryan@outlook.com",
                                        Subject = $"Invoice {convertedInvoice.number} has been {invoiceStatus} by {name}",
                                        OrderId = order.OrderId,
                                        MessageBody = $@"<p>Hi Support Team,</p><p>Invoice {convertedInvoice.number} <a href='{order.UpfrontInvoiceLink}' target='_blank'>can be viewed here.</a> The order <a href='https://ops.acceleratenetworks.com/Home/Order/{order.OrderId}' target='_blank'>can be edited here</a>, please follow up with the customer to set an install date.</p><p>Have a great day, hombre! 🤠</p>"
                                    };

                                    // Send the message the email server.
                                    var checkSend = await message.SendEmailAsync(emailUsername, emailPassword).ConfigureAwait(false);

                                    // If it didn't work try it again.
                                    if (!checkSend)
                                    {
                                        checkSend = await message.SendEmailAsync(emailUsername, emailPassword).ConfigureAwait(false);
                                    }

                                    // Mark it as sent.
                                    message.DateSent = DateTime.Now;
                                    message.DoNotSend = false;
                                    message.Completed = checkSend;

                                    // Update the database with the email's new status.
                                    var checkSave = await message.PostAsync(postgresql).ConfigureAwait(false);

                                    // Log the success or failure of the operation.
                                    if (checkSend && checkSave)
                                    {
                                        Log.Information($"[Quote Conversion] Successfully sent out email {message.EmailId} for order {order.OrderId}.");
                                    }
                                    else
                                    {
                                        Log.Fatal($"[Quote Conversion] Failed to sent out the email {message.EmailId} for order {order.OrderId}.");
                                    }

                                }
                            }
                            else
                            {
                                var error = await ex.GetResponseStringAsync();
                                Log.Error(error);
                            }
                        }
                    }

                    // This is too hard we'll deal with it later
                    //if (!string.IsNullOrWhiteSpace(order.BillingInvoiceReoccuringId))
                    //{
                    //    var reoccurringQuote = Invoice.GetQuoteByIdAsync(order.BillingInvoiceReoccuringId, _mvcConfiguration.InvoiceNinjaToken);
                    //    var reoccurring = ReccurringInvoice.GetByIdAsync(order.BillingInvoiceReoccuringId, _mvcConfiguration.InvoiceNinjaToken);
                    //}
                }
                catch (FlurlHttpException ex)
                {
                    var error = await ex.GetResponseStringAsync();
                    Log.Error(error);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    Log.Error(ex.StackTrace ?? "No stack trace found.");
                }
            }
        }

        public async static Task CheckForInvoicePaymentAsync(string postgresql, string invoiceNinjaToken, string emailUsername, string emailPassword)
        {
            Log.Information($"[Invoice Payment] Looking for invoices that were paid in the billing system.");

            var orders = await Order.GetAllAsync(postgresql).ConfigureAwait(false);

            // Don't both checking orders that are from before we upgraded to the current version of invoiceNinja.
            foreach (var order in orders.Where(x => x.DateSubmitted > DateTime.Parse("02/01/2023")))
            {
                // Get the quotes in invoice ninja and see if they've been converted
                try
                {
                    if (!string.IsNullOrWhiteSpace(order.BillingInvoiceId) && order.DateUpfrontInvoicePaid is null)
                    {
                        var upfrontInvoice = await Invoice.GetByIdAsync(order.BillingInvoiceId, invoiceNinjaToken);

                        if (upfrontInvoice is not null && upfrontInvoice.id == order.BillingInvoiceId && !string.IsNullOrWhiteSpace(upfrontInvoice.id))
                        {
                            if (upfrontInvoice.status_id is "4")
                            {
                                // mark the upfront invoice as paid and update the link
                                order.DateUpfrontInvoicePaid = DateTime.Now;
                            }

                            order.BillingClientId = upfrontInvoice.client_id;
                            order.BillingInvoiceId = upfrontInvoice.id;
                            if (order.Quote is true)
                            {
                                order.Quote = false;
                                order.DateConvertedFromQuote = DateTime.Now;
                            }
                            string newUpfrontLink = upfrontInvoice.invitations.FirstOrDefault()?.link ?? string.Empty;
                            order.UpfrontInvoiceLink = string.IsNullOrWhiteSpace(newUpfrontLink) ? order.UpfrontInvoiceLink : newUpfrontLink;
                            var checkUpdate = await order.PutAsync(postgresql);

                            // Only send the email of the invoice has actually been paid.
                            if (order.DateUpfrontInvoicePaid is not null && upfrontInvoice.status_id is "4")
                            {
                                string name = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName;
                                var message = new Email
                                {
                                    SalesEmailAddress = string.IsNullOrWhiteSpace(order.SalesEmail) ? string.Empty : order.SalesEmail,
                                    PrimaryEmailAddress = "support@acceleratenetworks.com",
                                    CarbonCopy = "thomas.ryan@outlook.com",
                                    Subject = $"Invoice {upfrontInvoice.number} has been paid by {name}",
                                    OrderId = order.OrderId,
                                    MessageBody = $@"<p>Hi Support Team,</p><p>Invoice {upfrontInvoice.number} <a href='{order.UpfrontInvoiceLink}' target='_blank'>can be viewed here.</a> The order <a href='https://ops.acceleratenetworks.com/Home/Order/{order.OrderId}' target='_blank'>can be edited here</a>, please follow up with the customer to set an install date.</p><p>Have a great day, hombre! 🤠</p>"
                                };

                                // Send the message the email server.
                                var checkSend = await message.SendEmailAsync(emailUsername, emailPassword).ConfigureAwait(false);

                                // If it didn't work try it again.
                                if (!checkSend)
                                {
                                    checkSend = await message.SendEmailAsync(emailUsername, emailPassword).ConfigureAwait(false);
                                }

                                // Mark it as sent.
                                message.DateSent = DateTime.Now;
                                message.DoNotSend = false;
                                message.Completed = checkSend;

                                // Update the database with the email's new status.
                                var checkSave = await message.PostAsync(postgresql).ConfigureAwait(false);

                                // Log the success or failure of the operation.
                                if (checkSend && checkSave)
                                {
                                    Log.Information($"[Invoice Payment] Successfully sent out email {message.EmailId} for order {order.OrderId}.");
                                }
                                else
                                {
                                    Log.Fatal($"[Invoice Payment] Failed to sent out the email {message.EmailId} for order {order.OrderId}.");
                                }
                            }
                        }
                    }

                    // This is too hard we'll deal with it later
                    //if (!string.IsNullOrWhiteSpace(order.BillingInvoiceReoccuringId))
                    //{
                    //    var reoccurringQuote = Invoice.GetQuoteByIdAsync(order.BillingInvoiceReoccuringId, _mvcConfiguration.InvoiceNinjaToken);
                    //    var reoccurring = ReccurringInvoice.GetByIdAsync(order.BillingInvoiceReoccuringId, _mvcConfiguration.InvoiceNinjaToken);
                    //}
                }
                catch (FlurlHttpException ex)
                {
                    var error = await ex.GetResponseStringAsync();
                    Log.Error(error);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    Log.Error(ex.StackTrace ?? "No stack trace found.");
                }
            }
        }
    }
}
