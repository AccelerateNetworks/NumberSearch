using BulkVS;

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
                                "[Background Worker] Queued Background Task {order.OrderId} is starting.", order.OrderId);

                            while (!token.IsCancellationRequested)
                            {
                                var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                // Create a single PIN for this order.
                                var random = new Random();
                                var pin = random.Next(100000, 99999999);

                                foreach (var productOrder in productOrders)
                                {
                                    if (!string.IsNullOrWhiteSpace(productOrder.DialedNumber))
                                    {
                                        try
                                        {
                                            var nto = await PhoneNumber.GetAsync(productOrder.DialedNumber, _postgresql).ConfigureAwait(false);

                                            if (nto is not null && !nto.Purchased)
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
                                                            PortoutPin = pin.ToString(new CultureInfo("en-US")),
                                                            TrunkGroup = "SFO",
                                                            Sms = true,
                                                            Mms = false
                                                        };

                                                        var orderResponse = await executeOrder.PostAsync(_bulkVSusername, _bulkVSpassword).ConfigureAwait(false);

                                                        nto.Purchased = true;
                                                        var verifyOrder = new PurchasedPhoneNumber
                                                        {
                                                            OrderId = order.OrderId,
                                                            DateOrdered = order.DateSubmitted,
                                                            DialedNumber = nto.DialedNumber,
                                                            DateIngested = nto.DateIngested,
                                                            IngestedFrom = nto.IngestedFrom,
                                                            // Keep the raw response as a receipt.
                                                            OrderResponse = JsonSerializer.Serialize(orderResponse),
                                                            // If the status code of the order comes back as 200 then it was sucessful.
                                                            Completed = orderResponse.Failed is null
                                                        };

                                                        var checkVerifyOrder = verifyOrder.PostAsync(_postgresql);
                                                        var checkMarkPurchased = nto.PutAsync(_postgresql);

                                                        await Task.WhenAll(new List<Task<bool>> { checkVerifyOrder, checkMarkPurchased }).ConfigureAwait(false);
                                                    }
                                                    else if (nto.IngestedFrom == "TeleMessage")
                                                    {
                                                        // Buy it and save the reciept.
                                                        var executeOrder = await DidsOrder.GetAsync(nto.DialedNumber, _CallFlow, _ChannelGroup, _teleToken).ConfigureAwait(false);

                                                        nto.Purchased = true;
                                                        var verifyOrder = new PurchasedPhoneNumber
                                                        {
                                                            OrderId = order.OrderId,
                                                            DateOrdered = order.DateSubmitted,
                                                            DialedNumber = nto.DialedNumber,
                                                            DateIngested = nto.DateIngested,
                                                            IngestedFrom = nto.IngestedFrom,
                                                            // Keep the raw response as a receipt.
                                                            OrderResponse = JsonSerializer.Serialize(executeOrder),
                                                            // If the status code of the order comes back as 200 then it was sucessful.
                                                            Completed = executeOrder.code == 200
                                                        };

                                                        var checkVerifyOrder = verifyOrder.PostAsync(_postgresql);
                                                        var checkMarkPurchased = nto.PutAsync(_postgresql);

                                                        // Set a note for these number purchases inside of Tele's system.
                                                        var getTeleId = await UserDidsGet.GetAsync(nto.DialedNumber, _teleToken).ConfigureAwait(false);
                                                        var setTeleLabel = await UserDidsNote.SetNote($"{order?.BusinessName} {order?.FirstName} {order?.LastName}", getTeleId.data.id, _teleToken).ConfigureAwait(false);

                                                        if (setTeleLabel.code == 200)
                                                        {
                                                            Log.Information($"Sucessfully set TeleMessage label for {nto.DialedNumber} to {order?.BusinessName} {order?.FirstName} {order?.LastName}.");
                                                        }
                                                        else
                                                        {
                                                            Log.Fatal($"Failed to set TeleMessage label for {nto.DialedNumber} to {order?.BusinessName} {order?.FirstName} {order?.LastName}.");
                                                        }

                                                        await Task.WhenAll(new List<Task<bool>> { checkVerifyOrder, checkMarkPurchased }).ConfigureAwait(false);

                                                    }
                                                    else if (nto.IngestedFrom == "FirstPointCom")
                                                    {
                                                        // Buy it and save the reciept.
                                                        var executeOrder = await FirstPointComOrderPhoneNumber.PostAsync(nto.DialedNumber, _fpcusername, _fpcpassword).ConfigureAwait(false);

                                                        nto.Purchased = true;
                                                        var verifyOrder = new PurchasedPhoneNumber
                                                        {
                                                            OrderId = order.OrderId,
                                                            DateOrdered = order.DateSubmitted,
                                                            DialedNumber = nto.DialedNumber,
                                                            DateIngested = nto.DateIngested,
                                                            IngestedFrom = nto.IngestedFrom,
                                                            // Keep the raw response as a receipt.
                                                            OrderResponse = JsonSerializer.Serialize(executeOrder),
                                                            // If the status code of the order comes back as 200 then it was sucessful.
                                                            Completed = executeOrder.code == 0
                                                        };

                                                        var checkVerifyOrder = verifyOrder.PostAsync(_postgresql);
                                                        var checkMarkPurchased = nto.PutAsync(_postgresql);

                                                        await Task.WhenAll(new List<Task<bool>> { checkVerifyOrder, checkMarkPurchased }).ConfigureAwait(false);
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

                                Log.Information(
                                    "[Background Worker] Queued Background Task {order.OrderId} is Completed.", order.OrderId);

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
