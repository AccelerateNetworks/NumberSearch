using FirstCom;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.TeleMesssage;

using Serilog;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NumberSearch.Mvc
{
    public class MonitorLoop
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly CancellationToken _cancellationToken;
        private readonly IConfiguration _configuration;
        private readonly Guid _teleToken;
        private readonly string _postgresql;
        private readonly int _CallFlow;
        private readonly int _ChannelGroup;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _fpcusername;
        private readonly string _fpcpassword;
        private readonly string _bulkVSusername;
        private readonly string _bulkVSpassword;
        private readonly string _emailUsername;
        private readonly string _emailPassword;

        public MonitorLoop(IBackgroundTaskQueue taskQueue,
            ILogger<MonitorLoop> logger,
            IHostApplicationLifetime applicationLifetime, IConfiguration configuration)
        {
            _taskQueue = taskQueue;
            _cancellationToken = applicationLifetime.ApplicationStopping;
            _configuration = configuration;
            _teleToken = Guid.Parse(configuration.GetConnectionString("TeleAPI"));
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
            _ = int.TryParse(_configuration.GetConnectionString("CallFlow"), out _CallFlow);
            _ = int.TryParse(_configuration.GetConnectionString("ChannelGroup"), out _ChannelGroup);
            _apiKey = configuration.GetConnectionString("BulkVSAPIKEY");
            _apiSecret = configuration.GetConnectionString("BulkVSAPISecret");
            _fpcusername = configuration.GetConnectionString("PComNetUsername");
            _fpcpassword = configuration.GetConnectionString("PComNetPassword");
            _bulkVSusername = configuration.GetConnectionString("BulkVSUsername");
            _bulkVSpassword = configuration.GetConnectionString("BulkVSPassword");
            _emailUsername = _configuration.GetConnectionString("SmtpUsername");
            _emailPassword = _configuration.GetConnectionString("SmtpPassword");
        }

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
                        _taskQueue.QueueBackgroundWorkItem(async token =>
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
                                                        // Buy it and save the reciept.
                                                        var executeOrder = new OrderTnRequestBody
                                                        {
                                                            TN = nto.DialedNumber,
                                                            Lidb = "Accelerate Networks",
                                                            PortoutPin = productOrder.PIN,
                                                            TrunkGroup = "SFO",
                                                            Sms = true,
                                                            Mms = false
                                                        };

                                                        var orderResponse = await executeOrder.PostAsync(_bulkVSusername, _bulkVSpassword).ConfigureAwait(false);

                                                        nto.Purchased = true;
                                                        productOrder.DateOrdered = DateTime.Now;
                                                        // Keep the raw response as a receipt.
                                                        productOrder.OrderResponse = JsonSerializer.Serialize(orderResponse);
                                                        // If the status code of the order comes back as 200 then it was sucessful.
                                                        productOrder.Completed = orderResponse.Failed is null;

                                                        var checkVerifyOrder = await productOrder.PutAsync(_postgresql).ConfigureAwait(false);
                                                        var checkMarkPurchased = await nto.PutAsync(_postgresql).ConfigureAwait(false);

                                                        Log.Information($"[Background Worker] Purchased number {nto.DialedNumber} from BulkVS.");
                                                    }
                                                    else if (nto.IngestedFrom == "TeleMessage")
                                                    {
                                                        // Buy it and save the reciept.
                                                        var executeOrder = await DidsOrder.GetAsync(nto.DialedNumber, _CallFlow, _ChannelGroup, _teleToken).ConfigureAwait(false);

                                                        nto.Purchased = true;
                                                        productOrder.DateOrdered = DateTime.Now;
                                                        productOrder.OrderResponse = JsonSerializer.Serialize(executeOrder);
                                                        productOrder.Completed = executeOrder.code == 200;

                                                        var checkVerifyOrder = await productOrder.PutAsync(_postgresql).ConfigureAwait(false);
                                                        var checkMarkPurchased = await nto.PutAsync(_postgresql).ConfigureAwait(false);

                                                        Log.Information($"[Background Worker] Purchased number {nto.DialedNumber} from TeliMessage.");

                                                        // Set a note for these number purchases inside of Tele's system.
                                                        var getTeleId = await UserDidsGet.GetAsync(nto.DialedNumber, _teleToken).ConfigureAwait(false);
                                                        var setTeleLabel = await UserDidsNote.SetNote($"{order?.BusinessName} {order?.FirstName} {order?.LastName}", getTeleId.data.id, _teleToken).ConfigureAwait(false);

                                                        if (setTeleLabel.code == 200)
                                                        {
                                                            Log.Information($"[Background Worker] Sucessfully set TeleMessage label for {nto.DialedNumber} to {order?.BusinessName} {order?.FirstName} {order?.LastName}.");
                                                        }
                                                        else
                                                        {
                                                            Log.Fatal($"[Background Worker] Failed to set TeleMessage label for {nto.DialedNumber} to {order?.BusinessName} {order?.FirstName} {order?.LastName}.");
                                                        }
                                                    }
                                                    else if (nto.IngestedFrom == "FirstPointCom")
                                                    {
                                                        // Buy it and save the reciept.
                                                        var executeOrder = await FirstPointComOrderPhoneNumber.PostAsync(nto.DialedNumber, _fpcusername, _fpcpassword).ConfigureAwait(false);

                                                        nto.Purchased = true;
                                                        productOrder.DateOrdered = DateTime.Now;
                                                        productOrder.OrderResponse = JsonSerializer.Serialize(executeOrder);
                                                        productOrder.Completed = executeOrder.code == 0;

                                                        var checkVerifyOrder = await productOrder.PutAsync(_postgresql).ConfigureAwait(false);
                                                        var checkMarkPurchased = await nto.PutAsync(_postgresql).ConfigureAwait(false);

                                                        Log.Information($"[Background Worker] Purchased number {nto.DialedNumber} from FirstPointCom.");
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Log.Fatal($"[Background Worker] Failed to purchase number {nto.DialedNumber}");
                                                    Log.Fatal($"[Background Worker] {ex.Message}");
                                                    Log.Fatal($"[Background Worker] {ex.InnerException}");
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
                                var emails = await Email.GetByOrderAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                Log.Information($"[Background Worker] {emails.Count()} emails to send.");

                                foreach (var message in emails)
                                {
                                    if (message.Completed)
                                    {
                                        continue;
                                    }

                                    if (!string.IsNullOrWhiteSpace(order.SalesEmail))
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
                                    message.Completed = checkSend;

                                    // Update the database with the email's new status.
                                    var checkSave = await message.PutAsync(_postgresql).ConfigureAwait(false);

                                    // Log the success or failure of the operation.
                                    if (checkSend && checkSave)
                                    {
                                        Log.Information($"[Background Worker] Sucessfully sent out email {message.EmailId} for order {order.OrderId}.");
                                    }
                                    else
                                    {
                                        Log.Fatal($"[Background Worker] Failed to sent out the email {message.EmailId} for order {order.OrderId}.");
                                    }
                                }

                                Log.Information(
                                    $"[Background Worker] Queued Background Task {order.OrderId} is Completed.");

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
