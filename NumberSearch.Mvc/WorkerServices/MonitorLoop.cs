using FirstCom.Models;

using Flurl.Http;

using Microsoft.Extensions.Hosting;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.Models;
using NumberSearch.Mvc.Models;

using Serilog;

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.WorkerServices
{
    public class MonitorLoop(IBackgroundTaskQueue taskQueue,
        IHostApplicationLifetime applicationLifetime, MvcConfiguration mvcConfiguration)
    {
        private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;
        private readonly string _postgresql = mvcConfiguration.PostgresqlProd;
        private readonly string _fpcusername = mvcConfiguration.PComNetUsername;
        private readonly string _fpcpassword = mvcConfiguration.PComNetPassword;
        private readonly string _bulkVSusername = mvcConfiguration.BulkVSUsername;
        private readonly string _bulkVSpassword = mvcConfiguration.BulkVSPassword;
        private readonly string _emailUsername = mvcConfiguration.SmtpUsername;
        private readonly string _emailPassword = mvcConfiguration.SmtpPassword;

        public void StartMonitorLoop()
        {
            Log.Information("[Background Worker] Monitor Loop is starting.");

            // Run a console user input loop in a background thread
            var monitor = Task.Run(async () => await MonitorAsync().ConfigureAwait(false));
        }

        public async Task MonitorAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(5000).ConfigureAwait(false);

                var orders = await Order.GetByBackGroundworkNotCompletedAsync(_postgresql);
                if (orders is not null && orders.Any())
                {
                    foreach (var order in orders)
                    {
                        // Enqueue a background work item to purchase all the numbers in this order.
                        taskQueue.QueueBackgroundWorkItem(async token =>
                        {
                            Log.Information(
                                "[Background Worker] Queued Background Task {OrderId} is starting.", order.OrderId);

                            while (!token.IsCancellationRequested)
                            {
                                // Execute the phone number purchases required for this order.
                                var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql);

                                Log.Information("[Background Worker] {Count} phone numbers to purchase.", purchasedPhoneNumbers.Count());

                                foreach (var productOrder in purchasedPhoneNumbers)
                                {
                                    if (!string.IsNullOrWhiteSpace(productOrder.DialedNumber))
                                    {
                                        try
                                        {
                                            var nto = await PhoneNumber.GetAsync(productOrder.DialedNumber, _postgresql);

                                            if (nto is not null && !productOrder.Completed)
                                            {
                                                try
                                                {
                                                    if (nto.IngestedFrom == "BulkVS")
                                                    {
                                                        // Buy it and save the receipt.
                                                        var executeOrder = new OrderTnRequestBody(
                                                            nto.DialedNumber,
                                                            "Accelerate Networks",
                                                            productOrder.PIN,
                                                            "SFO",
                                                            false,
                                                            false
                                                        );

                                                        var orderResponse = await executeOrder.PostAsync(_bulkVSusername, _bulkVSpassword);

                                                        nto.Purchased = string.IsNullOrWhiteSpace(orderResponse.Failed.Description) && orderResponse.Status is "Active";
                                                        productOrder.DateOrdered = DateTime.Now;
                                                        // Keep the raw response as a receipt.
                                                        productOrder.OrderResponse = orderResponse.RawResponse ?? JsonSerializer.Serialize(orderResponse);
                                                        // If the status code of the order comes back as 200 then it was successful.
                                                        productOrder.Completed = string.IsNullOrWhiteSpace(orderResponse.Failed.Description);

                                                        var checkVerifyOrder = await productOrder.PutAsync(_postgresql);
                                                        var checkMarkPurchased = await nto.PutAsync(_postgresql);

                                                        Log.Information("[Background Worker] Purchased number {DialedNumber} from BulkVS.", nto.DialedNumber);
                                                    }
                                                    else if (nto.IngestedFrom == "FirstPointCom")
                                                    {
                                                        // Buy it and save the receipt.
                                                        var executeOrder = await FirstPointCom.OrderPhoneNumberAsync($"1{nto.DialedNumber}".AsMemory(), _fpcusername.AsMemory(), _fpcpassword.AsMemory());

                                                        nto.Purchased = executeOrder.code == 0;
                                                        productOrder.DateOrdered = DateTime.Now;
                                                        productOrder.OrderResponse = JsonSerializer.Serialize(executeOrder);
                                                        productOrder.Completed = executeOrder.code == 0;

                                                        var checkVerifyOrder = await productOrder.PutAsync(_postgresql);
                                                        var checkMarkPurchased = await nto.PutAsync(_postgresql);

                                                        Log.Information("[Background Worker] Purchased number {DialedNumber} from FirstPointCom.", nto.DialedNumber);
                                                    }
                                                    else if (nto.IngestedFrom == "OwnedNumber")
                                                    {
                                                        var ownedNumber = await OwnedPhoneNumber.GetByDialedNumberAsync(nto.DialedNumber, _postgresql);

                                                        if (ownedNumber is not null)
                                                        {
                                                            // We already own it.
                                                            nto.Purchased = true;
                                                            productOrder.DateOrdered = DateTime.Now;
                                                            productOrder.OrderResponse = JsonSerializer.Serialize(ownedNumber);
                                                            productOrder.OrderResponse = "We already own this number.";
                                                            productOrder.Completed = true;
                                                            ownedNumber.BillingClientId = order?.BillingClientId ?? string.Empty;
                                                            ownedNumber.Notes = $"Purchased in Order {order?.OrderId}";
                                                            ownedNumber.OwnedBy = string.IsNullOrWhiteSpace(order?.BusinessName) ? $"{order?.FirstName} {order?.LastName}" : order?.BusinessName ?? string.Empty;

                                                            var checkVerifyOrder = await productOrder.PutAsync(_postgresql);
                                                            var checkMarkPurchased = await nto.PutAsync(_postgresql);
                                                            var checkOwnedNumber = await ownedNumber.PutAsync(_postgresql);

                                                            Log.Information("[Background Worker] Purchased number {DialedNumber} from OwnedNumbers.", nto?.DialedNumber);
                                                        }
                                                        else
                                                        {
                                                            Log.Information("[Background Worker] Failed to purchase number {DialedNumber} from OwnedNumbers as could not be found in the database.", nto?.DialedNumber);
                                                        }
                                                    }
                                                }
                                                catch (FlurlHttpException ex)
                                                {
                                                    Log.Fatal("[Background Worker] Failed to purchase number {DialedNumber}", nto.DialedNumber);
                                                    Log.Fatal("[Background Worker] {Message}", await ex.GetResponseStringAsync());
                                                }
                                                catch (Exception ex)
                                                {
                                                    Log.Fatal("[Background Worker] Failed to purchase number {DialedNumber}", nto.DialedNumber);
                                                    Log.Fatal("[Background Worker] {Message}", ex.Message);
                                                    Log.Fatal("[Background Worker] {StackTrace}", ex.StackTrace);
                                                }
                                            }
                                        }
                                        catch (OperationCanceledException)
                                        {
                                            // Prevent throwing if the Delay is cancelled
                                        }
                                    }
                                }

                                // Send out the confirmation emails.
                                var emails = await Email.GetByOrderAsync(order?.OrderId ?? Guid.NewGuid(), _postgresql);
                                Log.Information("[Background Worker] {Count} emails to send.", emails.Count());

                                foreach (var message in emails)
                                {
                                    if (message.Completed || message.DoNotSend)
                                    {
                                        continue;
                                    }

                                    if (!string.IsNullOrWhiteSpace(order?.SalesEmail))
                                    {
                                        message.SalesEmailAddress = order.SalesEmail;
                                    }

                                    // Send the message the email server.
                                    var checkSend = await message.SendEmailAsync(_emailUsername, _emailPassword);

                                    // If it didn't work try it again.
                                    if (!checkSend)
                                    {
                                        checkSend = await message.SendEmailAsync(_emailUsername, _emailPassword);
                                    }

                                    // Mark it as sent.
                                    message.DateSent = DateTime.Now;
                                    message.DoNotSend = false;
                                    message.Completed = checkSend;

                                    // Update the database with the email's new status.
                                    var checkSave = await message.PutAsync(_postgresql);

                                    // Log the success or failure of the operation.
                                    if (checkSend && checkSave)
                                    {
                                        Log.Information("[Background Worker] Successfully sent out email {EmailId} for order {OrderId}.", message.EmailId, order?.OrderId);
                                    }
                                    else
                                    {
                                        Log.Fatal("[Background Worker] Failed to sent out the email {EmailId} for order {OrderId}.", message.EmailId, order?.OrderId);
                                    }
                                }

                                Log.Information(
                                    "[Background Worker] Queued Background Task {OrderId} is Completed.", order?.OrderId);

                                break;
                            }
                        });

                        // To prevent repeated queuing.
                        order.BackgroundWorkCompleted = true;
                        var checkUpdate = order.PutAsync(_postgresql);
                    }
                }
                else
                {
                    await Task.Delay(5000).ConfigureAwait(false);
                }
            }
        }
    }
}
