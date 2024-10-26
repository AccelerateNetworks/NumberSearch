using FirstCom;

using Flurl.Http;

using Microsoft.Extensions.Hosting;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
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

                var orders = await Order.GetByBackGroundworkNotCompletedAsync(_postgresql).ConfigureAwait(false);
                if (orders is not null && orders.Any())
                {
                    foreach (var order in orders)
                    {
                        // Enqueue a background work item to purchase all the numbers in this order.
                        taskQueue.QueueBackgroundWorkItem(async token =>
                        {
                            Log.Information(
                                $"[Background Worker] Queued Background Task {order.OrderId} is starting.");

                            while (!token.IsCancellationRequested)
                            {
                                // Execute the phone number purchases required for this order.
                                var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                Log.Information($"[Background Worker] {purchasedPhoneNumbers.Count()} phone numbers to purchase.");

                                foreach (var productOrder in purchasedPhoneNumbers)
                                {
                                    if (!string.IsNullOrWhiteSpace(productOrder.DialedNumber))
                                    {
                                        try
                                        {
                                            var nto = await PhoneNumber.GetAsync(productOrder.DialedNumber, _postgresql).ConfigureAwait(false);

                                            if (nto is not null && !productOrder.Completed)
                                            {
                                                try
                                                {
                                                    if (nto.IngestedFrom == "BulkVS")
                                                    {
                                                        // Buy it and save the receipt.
                                                        var executeOrder = new OrderTnRequestBody
                                                        {
                                                            TN = nto.DialedNumber,
                                                            Lidb = "Accelerate Networks",
                                                            PortoutPin = productOrder.PIN,
                                                            TrunkGroup = "SFO",
                                                            Sms = false,
                                                            Mms = false
                                                        };

                                                        var orderResponse = await executeOrder.PostAsync(_bulkVSusername, _bulkVSpassword).ConfigureAwait(false);

                                                        nto.Purchased = string.IsNullOrWhiteSpace(orderResponse?.Failed?.Description);
                                                        productOrder.DateOrdered = DateTime.Now;
                                                        // Keep the raw response as a receipt.
                                                        productOrder.OrderResponse = JsonSerializer.Serialize(orderResponse);
                                                        // If the status code of the order comes back as 200 then it was successful.
                                                        productOrder.Completed = string.IsNullOrWhiteSpace(orderResponse?.Failed?.Description);

                                                        var checkVerifyOrder = await productOrder.PutAsync(_postgresql).ConfigureAwait(false);
                                                        var checkMarkPurchased = await nto.PutAsync(_postgresql).ConfigureAwait(false);

                                                        Log.Information($"[Background Worker] Purchased number {nto.DialedNumber} from BulkVS.");
                                                    }
                                                    else if (nto.IngestedFrom == "FirstPointCom")
                                                    {
                                                        // Buy it and save the receipt.
                                                        var executeOrder = await FirstPointComOrderPhoneNumber.PostAsync($"1{nto.DialedNumber}", _fpcusername, _fpcpassword).ConfigureAwait(false);

                                                        nto.Purchased = executeOrder.code == 0;
                                                        productOrder.DateOrdered = DateTime.Now;
                                                        productOrder.OrderResponse = JsonSerializer.Serialize(executeOrder);
                                                        productOrder.Completed = executeOrder.code == 0;

                                                        var checkVerifyOrder = await productOrder.PutAsync(_postgresql).ConfigureAwait(false);
                                                        var checkMarkPurchased = await nto.PutAsync(_postgresql).ConfigureAwait(false);

                                                        Log.Information($"[Background Worker] Purchased number {nto.DialedNumber} from FirstPointCom.");
                                                    }
                                                    else if (nto.IngestedFrom == "OwnedNumber")
                                                    {
                                                        var ownedNumber = await OwnedPhoneNumber.GetByDialedNumberAsync(nto.DialedNumber, _postgresql).ConfigureAwait(false);

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

                                                            var checkVerifyOrder = await productOrder.PutAsync(_postgresql).ConfigureAwait(false);
                                                            var checkMarkPurchased = await nto.PutAsync(_postgresql).ConfigureAwait(false);
                                                            var checkOwnedNumber = await ownedNumber.PutAsync(_postgresql).ConfigureAwait(false);

                                                            Log.Information($"[Background Worker] Purchased number {nto?.DialedNumber} from OwnedNumbers.");
                                                        }
                                                        else
                                                        {
                                                            Log.Information($"[Background Worker] Failed to purchase number {nto?.DialedNumber} from OwnedNumbers as could not be found in the database.");
                                                        }
                                                    }
                                                }
                                                catch (FlurlHttpException ex)
                                                {
                                                    Log.Fatal($"[Background Worker] Failed to purchase number {nto.DialedNumber}");
                                                    Log.Fatal($"[Background Worker] {await ex.GetResponseStringAsync()}");
                                                }
                                                catch (Exception ex)
                                                {
                                                    Log.Fatal($"[Background Worker] Failed to purchase number {nto.DialedNumber}");
                                                    Log.Fatal($"[Background Worker] {ex.Message}");
                                                    Log.Fatal($"[Background Worker] {ex.StackTrace}");
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
                                var emails = await Email.GetByOrderAsync(order?.OrderId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);
                                Log.Information($"[Background Worker] {emails.Count()} emails to send.");

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
                                    var checkSend = await message.SendEmailAsync(_emailUsername, _emailPassword).ConfigureAwait(false);

                                    // If it didn't work try it again.
                                    if (!checkSend)
                                    {
                                        checkSend = await message.SendEmailAsync(_emailUsername, _emailPassword).ConfigureAwait(false);
                                    }

                                    // Mark it as sent.
                                    message.DateSent = DateTime.Now;
                                    message.DoNotSend = false;
                                    message.Completed = checkSend;

                                    // Update the database with the email's new status.
                                    var checkSave = await message.PutAsync(_postgresql).ConfigureAwait(false);

                                    // Log the success or failure of the operation.
                                    if (checkSend && checkSave)
                                    {
                                        Log.Information($"[Background Worker] Successfully sent out email {message.EmailId} for order {order?.OrderId}.");
                                    }
                                    else
                                    {
                                        Log.Fatal($"[Background Worker] Failed to sent out the email {message.EmailId} for order {order?.OrderId}.");
                                    }
                                }

                                Log.Information(
                                    $"[Background Worker] Queued Background Task {order?.OrderId} is Completed.");

                                break;
                            }
                        });

                        // To prevent repeated queuing.
                        order.BackgroundWorkCompleted = true;
                        var checkUpdate = order.PutAsync(_postgresql).ConfigureAwait(false);
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
