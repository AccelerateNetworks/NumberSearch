using FirstCom;

using Flurl.Http;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.Call48;
using NumberSearch.DataAccess.Peerless;
using NumberSearch.DataAccess.TeliMesssage;

using Serilog;

using System;
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
        private readonly Guid _teleToken;
        private readonly string _postgresql;
        private readonly int _CallFlow;
        private readonly int _ChannelGroup;
        private readonly string _fpcusername;
        private readonly string _fpcpassword;
        private readonly string _bulkVSusername;
        private readonly string _bulkVSpassword;
        private readonly string _emailUsername;
        private readonly string _emailPassword;
        private readonly string _call48Username;
        private readonly string _call48Password;
        private readonly string _peerlessApiKey;

        public MonitorLoop(IBackgroundTaskQueue taskQueue,
            ILogger<MonitorLoop> logger,
            IHostApplicationLifetime applicationLifetime, IConfiguration configuration)
        {
            _taskQueue = taskQueue;
            _cancellationToken = applicationLifetime.ApplicationStopping;
            _ = Guid.TryParse(configuration.GetConnectionString("TeleAPI"), out _teleToken);
            _postgresql = configuration.GetConnectionString("PostgresqlProd");
            _ = int.TryParse(configuration.GetConnectionString("CallFlow"), out _CallFlow);
            _ = int.TryParse(configuration.GetConnectionString("ChannelGroup"), out _ChannelGroup);
            _fpcusername = configuration.GetConnectionString("PComNetUsername");
            _fpcpassword = configuration.GetConnectionString("PComNetPassword");
            _bulkVSusername = configuration.GetConnectionString("BulkVSUsername");
            _bulkVSpassword = configuration.GetConnectionString("BulkVSPassword");
            _call48Username = configuration.GetConnectionString("Call48Username");
            _call48Password = configuration.GetConnectionString("Call48Password");
            _emailUsername = configuration.GetConnectionString("SmtpUsername");
            _emailPassword = configuration.GetConnectionString("SmtpPassword");
            _peerlessApiKey = configuration.GetConnectionString("PeerlessAPIKey");
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
                                                            Sms = false,
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
                                                    else if (nto.IngestedFrom == "Call48")
                                                    {
                                                        // Find the number.
                                                        var credentials = await Login.LoginAsync(_call48Username, _call48Password).ConfigureAwait(false);

                                                        // Buy it and save the reciept.
                                                        var executeOrder = await Purchase.PurchasePhoneNumberAsync(nto, credentials.data.token).ConfigureAwait(false);

                                                        nto.Purchased = true;
                                                        productOrder.DateOrdered = DateTime.Now;
                                                        productOrder.OrderResponse = JsonSerializer.Serialize(executeOrder);
                                                        productOrder.Completed = executeOrder.code == 200;

                                                        var checkVerifyOrder = await productOrder.PutAsync(_postgresql).ConfigureAwait(false);
                                                        var checkMarkPurchased = await nto.PutAsync(_postgresql).ConfigureAwait(false);

                                                        Log.Information($"[Background Worker] Purchased number {nto.DialedNumber} from Call48.");
                                                    }
                                                    else if (nto.IngestedFrom == "Peerless")
                                                    {
                                                        var purchaseOrder = new DidOrderRequest
                                                        {
                                                            customer_name = "ACCELERN8230",
                                                            order_numbers = new OrderNumber[]
                                                            {
                                                                    new OrderNumber
                                                                    {
                                                                        did = nto.DialedNumber,
                                                                        connection_type = "trunk",
                                                                        trunk_name = "ACCELERN8230_SFO555930",
                                                                        extension_id = string.Empty,
                                                                        cnam_delivery = false,
                                                                        cnam_storage = false,
                                                                        cnam_storage_name = string.Empty,
                                                                        e911 = false,
                                                                        address = new { },
                                                                        directory_listing = new { },
                                                                    }
                                                            }
                                                        };

                                                        var checkPurchase = await purchaseOrder.PostAsync(_peerlessApiKey).ConfigureAwait(false);

                                                        nto.Purchased = true;
                                                        productOrder.DateOrdered = DateTime.Now;
                                                        productOrder.OrderResponse = JsonSerializer.Serialize(checkPurchase);
                                                        productOrder.Completed = !string.IsNullOrWhiteSpace(checkPurchase.order_id);

                                                        var checkVerifyOrder = await productOrder.PutAsync(_postgresql).ConfigureAwait(false);
                                                        var checkMarkPurchased = await nto.PutAsync(_postgresql).ConfigureAwait(false);

                                                        Log.Information($"[Background Worker] Purchased number {nto.DialedNumber} from Peerless.");
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
                                                    else if (nto.IngestedFrom == "OwnedNumber")
                                                    {
                                                        var ownedNumber = await OwnedPhoneNumber.GetByDialedNumberAsync(nto.DialedNumber, _postgresql).ConfigureAwait(false);
                                                        // We already own it.
                                                        nto.Purchased = true;
                                                        productOrder.DateOrdered = DateTime.Now;
                                                        productOrder.OrderResponse = JsonSerializer.Serialize(ownedNumber);
                                                        productOrder.OrderResponse = "We already own this number.";
                                                        productOrder.Completed = true;
                                                        ownedNumber.BillingClientId = order.BillingClientId;
                                                        ownedNumber.Notes = $"Purchased in Order {order.OrderId}";
                                                        ownedNumber.OwnedBy = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName;

                                                        var checkVerifyOrder = await productOrder.PutAsync(_postgresql).ConfigureAwait(false);
                                                        var checkMarkPurchased = await nto.PutAsync(_postgresql).ConfigureAwait(false);
                                                        var checkOwnedNumber = await ownedNumber.PutAsync(_postgresql).ConfigureAwait(false);

                                                        Log.Information($"[Background Worker] Purchased number {nto.DialedNumber} from OwnedNumbers.");
                                                    }


                                                    // Now that the number is purchased, register it as an offnet number with Teli.
                                                    var checkExists = await UserDidsGet.GetAsync(nto.DialedNumber, _teleToken).ConfigureAwait(false);

                                                    if (checkExists is null || checkExists?.code != 200)
                                                    {
                                                        var checkSubmit = await DidsOffnet.SubmitNumberAsync(nto.DialedNumber, _teleToken);

                                                        if (checkSubmit.code == 200)
                                                        {
                                                            Log.Information($"[Background Worker] Submitted {nto.DialedNumber} as an Offnet number to Teli.");
                                                        }
                                                        else
                                                        {
                                                            Log.Fatal($"[Background Worker] Failed to submit {nto.DialedNumber} as an Offnet number to Teli.");
                                                            Log.Fatal($"[Background Worker] {checkSubmit?.status} {checkSubmit?.error}");
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

                                // Now that the number has been submitted for porting, register it as an offnet number with Teli.
                                var portedPhoneNumber = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                foreach (var phoneNumber in portedPhoneNumber)
                                {
                                    if (!string.IsNullOrWhiteSpace(phoneNumber.ExternalPortRequestId))
                                    {
                                        var checkExists = await UserDidsGet.GetAsync(phoneNumber.PortedDialedNumber, _teleToken).ConfigureAwait(false);

                                        if (checkExists is null || checkExists?.code != 200)
                                        {
                                            var checkOffnet = await DidsOffnet.SubmitNumberAsync(phoneNumber.PortedDialedNumber, _teleToken).ConfigureAwait(false);

                                            if (checkOffnet.code == 200)
                                            {
                                                Log.Information($"[Background Worker] Submitted {phoneNumber.PortedDialedNumber} as an Offnet number to Teli.");
                                            }
                                            else
                                            {
                                                Log.Information($"[Background Worker] Failed to submit {phoneNumber.PortedDialedNumber} as an Offnet number to Teli.");
                                            }
                                        }
                                    }
                                }

                                // Send out the confirmation emails.
                                var emails = await Email.GetByOrderAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                Log.Information($"[Background Worker] {emails.Count()} emails to send.");

                                foreach (var message in emails)
                                {
                                    if (message.Completed || message.DoNotSend)
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
                                    message.DoNotSend = false;
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
