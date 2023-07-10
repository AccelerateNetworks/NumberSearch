using NumberSearch.DataAccess;

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

            foreach (var order in orders)
            {
                // Orders that should be marked as complete because the install data has passed?
                if (order.Quote is false && order.InstallDate is not null && order.Completed is false && DateTime.Now > order.InstallDate)
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
                else if (order.Quote is false && order.Completed is false && order.DateConvertedFromQuote >= DateTime.Now.AddDays(-1))
                {
                    ordersConvertedFromQuotesToday.Add(order);
                }
                // Orders completed today?
                else if (order.Quote is false && order.Completed is true && order.DateCompleted >= DateTime.Now.AddDays(-1))
                {
                    ordersCompletedToday.Add(order);
                }
                else if (order.Quote is false && order.Completed is false && order.DateSubmitted >= DateTime.Now.AddDays(-1))
                {
                    ordersSubmittedToday.Add(order);
                }
                else if (order.Quote is true && order.DateSubmitted >= DateTime.Now.AddDays(-1))
                {
                    quotesSubmittedToday.Add(order);
                }
            }
            var output = new StringBuilder();

            output.Append("<p>Hey Dan,</p><p>Here's everything you need to know about the orders in Accelerate Networks system.</p>");

            if (ordersCompletedToday.Count > 0)
            {
                output.Append("<p>Orders completed today:</p><ul>");

                foreach (var item in ordersCompletedToday)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
                    output.Append($"<li><a href='https://acceleratenetworks.com/cart/order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://acceleratenetworks.com/cart/order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
                }
                output.Append("</ul>");

            }

            if (ordersConvertedFromQuotesToday.Count > 0)
            {
                output.Append("<p>Converted from quotes today:</p>");

                foreach (var item in ordersConvertedFromQuotesToday)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
                }
                output.Append("</ul>");
            }

            if (ordersWithUnsubmittedPortRequests.Count > 0)
            {
                output.Append("<p>Unsubmitted port requests:</p>");

                foreach (var item in ordersWithUnsubmittedPortRequests)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a></li>");
                }
                output.Append("</ul>");
            }

            if (ordersWithUnfinishedPortRequests.Count > 0)
            {
                output.Append("<p>Unfinished port requests:</p>");

                foreach (var item in ordersWithUnfinishedPortRequests)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a></li>");
                }
                output.Append("</ul>");
            }

            if (ordersWithCompletedPortRequests.Count > 0)
            {
                output.Append("<p>Uncompleted orders with completed port requests:</p>");

                foreach (var item in ordersWithCompletedPortRequests)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
                }
                output.Append("</ul>");
            }

            if (ordersToMarkCompleted.Count > 0)
            {
                output.Append("<p>Uncompleted orders where the install date has passed:</p><ul>");

                foreach (var item in ordersToMarkCompleted)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
                }
                output.Append("</ul>");
            }

            if (ordersSubmittedToday.Count > 0)
            {
                output.Append("<p>Orders submitted today:</p><ul>");

                foreach (var item in ordersSubmittedToday)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
                }
                output.Append("</ul>");
            }

            if (quotesSubmittedToday.Count > 0)
            {
                output.Append("<p>Quotes submitted today:</p><ul>");

                foreach (var item in quotesSubmittedToday)
                {
                    var orderName = string.IsNullOrWhiteSpace(item.BusinessName) ? $"{item.FirstName} {item.LastName}" : item.BusinessName;
                    var salesEmail = string.IsNullOrWhiteSpace(item.SalesEmail) ? "No sales rep assigned" : item.SalesEmail;
                    var installDate = item?.InstallDate is not null ? item?.InstallDate.GetValueOrDefault().ToShortDateString() : "No install date set";
                    output.Append($"<li><a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a> - <a href=\"mailto:{item.SalesEmail}?subject={orderName}&body=<a href='https://ops.acceleratenetworks.com/Home/Order/{item.OrderId}' target='_blank' rel='noopener noreferrer'>{orderName}</a>\">{salesEmail}</a> - {installDate}</li>");
                }
                output.Append("</ul>");
            }

            output.Append("<p>Have a great day, hombre! 🤠</p>");

            var notificationEmail = new Email
            {
                PrimaryEmailAddress = appConfig.EmailDan,
                CarbonCopy = appConfig.EmailTom,
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
    }
}
