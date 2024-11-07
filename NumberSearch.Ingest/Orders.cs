using Flurl.Http;

using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity.Data;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.InvoiceNinja;

using Org.BouncyCastle.Pqc.Crypto.Lms;

using Serilog;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using static NumberSearch.Ingest.Program;

namespace NumberSearch.Ingest
{
    public class Orders
    {
        public static async Task<IngestStatistics> EmailDailyAsync(Owned.SMSRouteChange[] smsRouteChanges, IngestConfiguration appConfig)
        {
            DateTime start = DateTime.Now;

            _ = await Orders.DailyBriefingEmailAsync(smsRouteChanges, appConfig);

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


            if (await combined.PostAsync(appConfig.Postgresql.ToString()))
            {
                Log.Information($"[DailyEmails] Sent out the emails {DateTime.Now}.");
            }
            else
            {
                Log.Fatal($"[DailyEmails] Failed to send out the emails {DateTime.Now}.");
            }

            return combined;
        }

        private static Task<AccessTokenResponse> GetTokenAsync(IngestConfiguration appConfig)
        {
            var loginRequest = new LoginRequest()
            {
                Email = appConfig.MessagingUsername.ToString(),
                Password = appConfig.MessagingPassword.ToString(),
                TwoFactorCode = string.Empty,
                TwoFactorRecoveryCode = string.Empty
            };
            return $"{appConfig.MessagingURL}login".PostJsonAsync(loginRequest).ReceiveJson<AccessTokenResponse>();
        }

        private class MessageRecord
        {
            [Key]
            public Guid Id { get; set; } = Guid.NewGuid();
            public string From { get; set; } = string.Empty;
            public string To { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string MediaURLs { get; set; } = string.Empty;
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public MessageType MessageType { get; set; }
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public MessageSource MessageSource { get; set; }
            // Convert to DateTimeOffset if db is not SQLite.
            [DataType(DataType.DateTime)]
            public DateTime DateReceivedUTC { get; set; } = DateTime.UtcNow;
            public string RawRequest { get; set; } = string.Empty;
            public string RawResponse { get; set; } = string.Empty;
            public bool Succeeded { get; set; } = false;
            public string ToForward { get; set; } = string.Empty;
        }

        private enum MessageType { SMS, MMS };
        private enum MessageSource { Incoming, Outgoing };

        public static async Task<bool> DailyBriefingEmailAsync(Owned.SMSRouteChange[] smsRouteChanges, IngestConfiguration appConfig)
        {
            // Gather all of the info to put into the daily email.
            var orders = await Order.GetAllAsync(appConfig.Postgresql.ToString());

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
            var failedMessages = Array.Empty<MessageRecord>();

            var token = await GetTokenAsync(appConfig);
            failedMessages = await $"{appConfig.MessagingURL}message/all/failed?start={DateTime.Now.AddDays(-3).ToShortDateString()}&end={DateTime.Now.AddDays(1).ToShortDateString()}".WithOAuthBearerToken(token.AccessToken).GetJsonAsync<MessageRecord[]>();

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
                    var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, appConfig.Postgresql.ToString());
                    if (portRequest is not null && portRequest?.State is "COMPLETE")
                    {
                        ordersWithCompletedPortRequests.Add(order);
                    }
                }
                // Orders that have unfinished port requests
                else if (order.Quote is false && order.Completed is false)
                {
                    var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, appConfig.Postgresql.ToString());
                    if (portRequest is not null && portRequest?.State is not "COMPLETE" && portRequest?.DateSubmitted > order.DateSubmitted)
                    {
                        ordersWithUnfinishedPortRequests.Add(order);
                    }
                }
                // Orders that have unsubmitted port requests
                else if (order.Quote is false && order.Completed is false)
                {
                    var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, appConfig.Postgresql.ToString());
                    if (portRequest is not null && portRequest?.State is not "COMPLETE" && portRequest?.DateSubmitted < order.DateSubmitted)
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
                else if (order.Quote is false && order.Completed is true && order.DateCompleted.HasValue is true && order.DateCompleted.Value >= DateTime.Now.AddDays(-7))
                {
                    ordersCompletedToday.Add(order);
                }
                else if (order.Quote is false && order.Completed is false && order.DateSubmitted >= DateTime.Now.AddDays(-7))
                {
                    ordersSubmittedToday.Add(order);
                }
                else if (order.Quote is true && order.DateSubmitted >= DateTime.Now.AddDays(-7))
                {
                    quotesSubmittedToday.Add(order);
                }
                else if (order.Quote is false && order.InstallDate is not null && DateTime.Now.Ticks > order.InstallDate.Value.AddDays(7).Ticks && DateTime.Now.Ticks < order.InstallDate.Value.AddDays(7).Ticks)
                {
                    oneWeekFollowUp.Add(order);
                }
                else if (order.Quote is false && order.InstallDate is not null && DateTime.Now.Ticks > order.InstallDate.Value.AddMonths(1).Ticks && DateTime.Now.Ticks < order.InstallDate.Value.AddMonths(1).AddDays(7).Ticks)
                {
                    oneMonthFollowUp.Add(order);
                }
                else if (order.Quote is false && order.InstallDate is not null && DateTime.Now.Ticks > order.InstallDate.Value.AddYears(1).Ticks && DateTime.Now.Ticks < order.InstallDate.Value.AddYears(1).AddDays(7).Ticks)
                {
                    yearlyFollowUp.Add(order);
                }
            }
            var output = new StringBuilder();

            output.Append("<p>Hey Support,</p><p>Here's everything you need to know about the orders in the Accelerate Networks system.</p>");

            output.Append("<p>Orders submitted this week:</p><ul>");
            if (ordersSubmittedToday.Count > 0)
            {
                foreach (var item in ordersSubmittedToday)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
                    var paid = item?.DateUpfrontInvoicePaid is not null && item.DateUpfrontInvoicePaid.Value > item.DateSubmitted ? $"Paid {item.DateUpfrontInvoicePaid.Value.ToShortDateString()}" : "Unpaid";
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item?.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName} - Install Date {installDate} - <strong>{paid}</strong></li>");
                }
                output.Append("</ul>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            output.Append("<p>Orders completed this week:</p><ul>");
            if (ordersCompletedToday.Count > 0)
            {
                foreach (var item in ordersCompletedToday)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var completedDate = item?.DateCompleted is not null ? item?.DateCompleted.GetValueOrDefault().ToShortDateString() : "No completed date set";
                    output.Append($"<li><a href='https://acceleratenetworks.com/cart/order/{item?.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> submitted on {item?.DateSubmitted.ToShortDateString()} - Completed on {completedDate}</li>");
                }
                output.Append("</ul>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            output.Append("<p>Quotes submitted this week:</p><ul>");
            if (quotesSubmittedToday.Count > 0)
            {
                foreach (var item in quotesSubmittedToday)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item?.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item?.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item?.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a></li>");
                }
                output.Append("</ul>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            //output.Append("<p>Converted from quotes today:</p><ul>");
            //if (ordersConvertedFromQuotesToday.Count > 0)
            //{
            //    foreach (var item in ordersConvertedFromQuotesToday)
            //    {
            //        var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
            //        var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
            //        var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
            //        output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
            //    }
            //    output.Append("</ul>");
            //}
            //else
            //{
            //    output.Append("<li>None</li></ul>");
            //}

            //output.Append("<p>Unsubmitted port requests:</p><ul>");
            //if (ordersWithUnsubmittedPortRequests.Count > 0)
            //{
            //    foreach (var item in ordersWithUnsubmittedPortRequests)
            //    {
            //        var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
            //        var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
            //        output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a></li>");
            //    }
            //    output.Append("</ul>");
            //}
            //else
            //{
            //    output.Append("<li>None</li></ul>");
            //}

            //output.Append("<p>Unfinished port requests:</p><ul>");
            //if (ordersWithUnfinishedPortRequests.Count > 0)
            //{
            //    foreach (var item in ordersWithUnfinishedPortRequests)
            //    {
            //        var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
            //        var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
            //        output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a></li>");
            //    }
            //    output.Append("</ul>");
            //}
            //else
            //{
            //    output.Append("<li>None</li></ul>");
            //}

            //output.Append("<p>Uncompleted orders with completed port requests:</p><ul>");
            //if (ordersWithCompletedPortRequests.Count > 0)
            //{
            //    foreach (var item in ordersWithCompletedPortRequests)
            //    {
            //        var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
            //        var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
            //        var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
            //        output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
            //    }
            //    output.Append("</ul>");
            //}
            //else
            //{
            //    output.Append("<li>None</li></ul>");
            //}

            output.Append("<p>Owned Numbers who's SMS Routing changed from yesterday:</p><ul>");
            if (smsRouteChanges.Length > 0)
            {
                foreach (var item in smsRouteChanges)
                {
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/OwnedNumbers/{item.DialedNumber}' target='_blank' rel='noopener noreferrer'>{item.DialedNumber} - Old Route: {item.OldRoute} - New Route: <strong>{item.NewRoute}</strong> - FirstCom Message: {item.Message}</li>");
                }
                output.Append("</ul>");
                output.Append("<p>You can investigate these numbers and refresh their Upstream Status, attempt to Reregister them, and look at their current Carrier in <a href='https://ops.acceleratenetworks.com/Messaging' target='_blank' rel='noopener noreferrer'>Ops => Messaging Users</a></p>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            output.Append("<p>Incomplete orders where the install date has passed in the last quarter:</p><ul>");
            if (ordersToMarkCompleted.Count > 0)
            {
                foreach (var item in ordersToMarkCompleted)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item?.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item?.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item?.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
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
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item?.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item?.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item?.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
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
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item?.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item?.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item?.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
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
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item?.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item?.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item?.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
                }
                output.Append("</ul>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            output.Append("<p>Failed outbound SMS/MMS messages:</p><ul>");
            if (failedMessages.Length > 0)
            {
                foreach (var item in failedMessages)
                {
                    output.Append($"<li>From {item.From} to {item.To} at {item.DateReceivedUTC}</li>");
                }
                output.Append("</ul>");
            }
            else
            {
                output.Append("<li>None</li></ul>");
            }

            output.Append("<p>Have a great day, hombre! 🤠</p>");

            var notificationEmail = new DataAccess.Models.Email
            {
                PrimaryEmailAddress = appConfig.EmailDan.ToString(),
                CarbonCopy = appConfig.EmailTom.ToString(),
                SalesEmailAddress = "support@acceleratenetworks.com",
                DateSent = DateTime.Now,
                Subject = $"[Ingest] Daily Briefing for {DateTime.Now.ToShortDateString()}",
                MessageBody = output.ToString(),
                OrderId = new Guid(),
                Completed = false
            };

            var checkSend = await notificationEmail.SendEmailAsync(appConfig.SmtpUsername.ToString(), appConfig.SmtpPassword.ToString());
            var checkSave = await notificationEmail.PostAsync(appConfig.Postgresql.ToString());

            return checkSend && checkSave;
        }

        public async static Task CheckForQuoteConversionsAsync(ReadOnlyMemory<char> postgresql, ReadOnlyMemory<char> invoiceNinjaToken, ReadOnlyMemory<char> emailUsername, ReadOnlyMemory<char> emailPassword)
        {
            Log.Information($"[Quote Conversion] Looking for quotes that were converted to invoices in the billing system.");

            var orders = await Order.GetAllQuotesAsync(postgresql.ToString());

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
                            InvoiceDatum upfront = await Invoice.GetQuoteByIdAsync(order.BillingInvoiceId, invoiceNinjaToken.ToString());

                            if ( upfront.id == order.BillingInvoiceId && !string.IsNullOrWhiteSpace(upfront.invoice_id))
                            {
                                var convertedInvoice = await Invoice.GetByIdAsync(upfront.invoice_id, invoiceNinjaToken.ToString());

                                string newUpfrontLink = convertedInvoice.invitations.FirstOrDefault().link;

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
                                var checkUpdate = await order.PutAsync(postgresql.ToString());
                                string name = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName;
                                var message = new DataAccess.Models.Email
                                {
                                    SalesEmailAddress = string.IsNullOrWhiteSpace(order.SalesEmail) ? string.Empty : order.SalesEmail,
                                    PrimaryEmailAddress = "dan@acceleratenetworks.com",
                                    CarbonCopy = "thomas.ryan@outlook.com",
                                    Subject = $"Quote {upfront.number} has been {invoiceStatus} by {name}",
                                    OrderId = order.OrderId,
                                    MessageBody = $@"<p>Hi Sales Team,</p><p>The new invoice {convertedInvoice.number} <a href='{order.UpfrontInvoiceLink}' target='_blank'>can be viewed here.</a> The order <a href='https://ops.acceleratenetworks.com/Home/Order/{order.OrderId}' target='_blank'>can be edited here</a>, please follow up with the customer to set an install date.</p><p>Have a great day, hombre! 🤠</p>"
                                };

                                // Send the message the email server.
                                var checkSend = await message.SendEmailAsync(emailUsername.ToString(), emailPassword.ToString());

                                // If it didn't work try it again.
                                if (!checkSend)
                                {
                                    checkSend = await message.SendEmailAsync(emailUsername.ToString(), emailPassword.ToString());
                                }

                                // Mark it as sent.
                                message.DateSent = DateTime.Now;
                                message.DoNotSend = false;
                                message.Completed = checkSend;

                                // Update the database with the email's new status.
                                var checkSave = await message.PostAsync(postgresql.ToString());

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
                                var convertedInvoice = await Invoice.GetByIdAsync(order.BillingInvoiceId, invoiceNinjaToken.ToString());

                                if (convertedInvoice.id == order.BillingInvoiceId && !string.IsNullOrWhiteSpace(convertedInvoice.id))
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
                                    string newUpfrontLink = convertedInvoice.invitations.FirstOrDefault().link;
                                    order.UpfrontInvoiceLink = string.IsNullOrWhiteSpace(newUpfrontLink) ? order.UpfrontInvoiceLink : newUpfrontLink;


                                    var checkUpdate = await order.PutAsync(postgresql.ToString());
                                    string name = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName;
                                    var invoiceStatus = convertedInvoice.status_id is "4" ? "paid" : "converted from a quote";
                                    var message = new DataAccess.Models.Email
                                    {
                                        SalesEmailAddress = string.IsNullOrWhiteSpace(order.SalesEmail) ? string.Empty : order.SalesEmail,
                                        PrimaryEmailAddress = "dan@acceleratenetworks.com",
                                        CarbonCopy = "thomas.ryan@outlook.com",
                                        Subject = $"Invoice {convertedInvoice.number} has been {invoiceStatus} by {name}",
                                        OrderId = order.OrderId,
                                        MessageBody = $@"<p>Hi Support Team,</p><p>Invoice {convertedInvoice.number} <a href='{order.UpfrontInvoiceLink}' target='_blank'>can be viewed here.</a> The order <a href='https://ops.acceleratenetworks.com/Home/Order/{order.OrderId}' target='_blank'>can be edited here</a>, please follow up with the customer to set an install date.</p><p>Have a great day, hombre! 🤠</p>"
                                    };

                                    // Send the message the email server.
                                    var checkSend = await message.SendEmailAsync(emailUsername.ToString(), emailPassword.ToString());

                                    // If it didn't work try it again.
                                    if (!checkSend)
                                    {
                                        checkSend = await message.SendEmailAsync(emailUsername.ToString(), emailPassword.ToString());
                                    }

                                    // Mark it as sent.
                                    message.DateSent = DateTime.Now;
                                    message.DoNotSend = false;
                                    message.Completed = checkSend;

                                    // Update the database with the email's new status.
                                    var checkSave = await message.PostAsync(postgresql.ToString());

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

        public async static Task CheckForInvoicePaymentAsync(ReadOnlyMemory<char> postgresql, ReadOnlyMemory<char> invoiceNinjaToken, ReadOnlyMemory<char> emailUsername, ReadOnlyMemory<char> emailPassword)
        {
            Log.Information($"[Invoice Payment] Looking for invoices that were paid in the billing system.");

            var orders = await Order.GetAllAsync(postgresql.ToString());

            // Don't both checking orders that are from before we upgraded to the current version of invoiceNinja.
            foreach (var order in orders.Where(x => x.DateSubmitted > DateTime.Parse("02/01/2023")))
            {
                // Get the quotes in invoice ninja and see if they've been converted
                try
                {
                    if (!string.IsNullOrWhiteSpace(order.BillingInvoiceId) && order.DateUpfrontInvoicePaid is null)
                    {
                        var upfrontInvoice = await Invoice.GetByIdAsync(order.BillingInvoiceId, invoiceNinjaToken.ToString());

                        if (upfrontInvoice.id == order.BillingInvoiceId && !string.IsNullOrWhiteSpace(upfrontInvoice.id))
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
                            string newUpfrontLink = upfrontInvoice.invitations.FirstOrDefault().link;
                            order.UpfrontInvoiceLink = string.IsNullOrWhiteSpace(newUpfrontLink) ? order.UpfrontInvoiceLink : newUpfrontLink;
                            var checkUpdate = await order.PutAsync(postgresql.ToString());

                            // Only send the email of the invoice has actually been paid.
                            if (order.DateUpfrontInvoicePaid is not null && upfrontInvoice.status_id is "4")
                            {
                                string name = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName;
                                var message = new DataAccess.Models.Email
                                {
                                    SalesEmailAddress = string.IsNullOrWhiteSpace(order.SalesEmail) ? string.Empty : order.SalesEmail,
                                    PrimaryEmailAddress = "support@acceleratenetworks.com",
                                    CarbonCopy = "thomas.ryan@outlook.com",
                                    Subject = $"Invoice {upfrontInvoice.number} has been paid by {name}",
                                    OrderId = order.OrderId,
                                    MessageBody = $@"<p>Hi Support Team,</p><p>Invoice {upfrontInvoice.number} <a href='{order.UpfrontInvoiceLink}' target='_blank'>can be viewed here.</a> The order <a href='https://ops.acceleratenetworks.com/Home/Order/{order.OrderId}' target='_blank'>can be edited here</a>, please follow up with the customer to set an install date.</p><p>Have a great day, hombre! 🤠</p>"
                                };

                                // Send the message the email server.
                                var checkSend = await message.SendEmailAsync(emailUsername.ToString(), emailPassword.ToString());

                                // If it didn't work try it again.
                                if (!checkSend)
                                {
                                    checkSend = await message.SendEmailAsync(emailUsername.ToString(), emailPassword.ToString());
                                }

                                // Mark it as sent.
                                message.DateSent = DateTime.Now;
                                message.DoNotSend = false;
                                message.Completed = checkSend;

                                // Update the database with the email's new status.
                                var checkSave = await message.PostAsync(postgresql.ToString());

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
                    Log.Error(ex.Message);
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
