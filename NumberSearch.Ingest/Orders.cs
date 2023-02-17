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
    }
}
