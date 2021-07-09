using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.TeleMesssage;

using Serilog;

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class PortRequests
    {
        public async static Task UpdateStatusesBulkVSAsync(IConfiguration configuration)
        {
            Log.Information("[BulkVS] [PortRequests] Ingesting Port Request statuses.");

            var postgresSQL = configuration.GetConnectionString("PostgresqlProd");
            var bulkVSusername = configuration.GetConnectionString("BulkVSUsername");
            var bulkVSpassword = configuration.GetConnectionString("BulkVSPassword");
            var smtpUsername = configuration.GetConnectionString("SmtpUsername");
            var smtpPassword = configuration.GetConnectionString("SmtpPassword");
            var emailOrders = configuration.GetConnectionString("EmailOrders");

            var portRequests = await PortRequest.GetAllAsync(postgresSQL).ConfigureAwait(false);
            var bulkVSPortRequests = portRequests.Where(x => (x.VendorSubmittedTo == "BulkVS" && x.Completed is false && x.DateSubmitted > DateTime.Now.AddYears(-3)));

            foreach (var request in bulkVSPortRequests)
            {
                var portedNumbers = await PortedPhoneNumber.GetByPortRequestIdAsync(request.PortRequestId, postgresSQL).ConfigureAwait(false);

                bool focChanged = false;
                bool portCompleted = false;

                foreach (var number in portedNumbers)
                {
                    // If the request has been assigned and external port request id than it has been submitted to the vendor.
                    if (!string.IsNullOrWhiteSpace(number.ExternalPortRequestId))
                    {
                        var bulkStatus = await PortTn.GetAsync(number.ExternalPortRequestId, bulkVSusername, bulkVSpassword).ConfigureAwait(false);

                        // If we get no numbers back for this order number skip it.
                        if (bulkStatus.TNList is null || !bulkStatus.TNList.Any())
                        {
                            continue;
                        }

                        var matchingNumber = bulkStatus.TNList.Where(x => x.TN == $"1{number.PortedDialedNumber}").FirstOrDefault();

                        if (matchingNumber is not null)
                        {
                            var checkRDDParse = DateTime.TryParse(matchingNumber.RDD, out var FOCDate);

                            // If the FOC Date has been changed update it.
                            if (checkRDDParse && (number.DateFirmOrderCommitment is null))
                            {
                                number.DateFirmOrderCommitment = FOCDate;
                            }
                            else if (checkRDDParse && (FOCDate != number.DateFirmOrderCommitment))
                            {
                                number.DateFirmOrderCommitment = FOCDate;
                                focChanged = true;
                            };

                            // If the porting status for this number has changed update it.
                            if (!string.IsNullOrWhiteSpace(matchingNumber.LNPStatus) && matchingNumber.LNPStatus != number.RequestStatus)
                            {
                                number.RequestStatus = matchingNumber.LNPStatus.Trim();
                                if (number.RequestStatus == "COMPLETE")
                                {
                                    portCompleted = true;
                                    number.Completed = true;
                                }
                            }

                            var checkPortedNumberUpdate = await number.PutAsync(postgresSQL).ConfigureAwait(false);
                            Log.Information($"[BulkVS] [PortRequests] Updated BulkVS Port Request {request?.TeliId} - {number?.PortedDialedNumber} - {number?.RequestStatus} - {number?.DateFirmOrderCommitment?.ToShortDateString()}");
                        }
                    }
                }

                // All of the statuses for all of the numbers.
                var numberStatuses = portedNumbers.Select(x => x.RequestStatus);

                var canceled = numberStatuses?.Where(x => x == "CANCELLED");
                var rejected = numberStatuses?.Where(x => x == "EXCEPTION");
                var completed = numberStatuses?.Where(x => x == "COMPLETE");

                // If all the numbers have been ported.
                if ((completed != null) && (completed.Any()) && (completed.Count() == numberStatuses.Count()))
                {
                    request.RequestStatus = "COMPLETE";
                    request.DateCompleted = DateTime.Now;
                    request.DateUpdated = DateTime.Now;
                    request.Completed = true;
                }
                // If the porting of a number has been canceled.
                else if ((canceled != null) && (canceled.Any()))
                {
                    request.RequestStatus = "CANCELLED";
                    request.DateCompleted = DateTime.Now;
                    request.DateUpdated = DateTime.Now;
                    request.Completed = false;
                }
                // If a request to port a number has been rejected.
                else if ((rejected != null) && (rejected.Any()))
                {
                    request.RequestStatus = "EXCEPTION";
                    request.DateCompleted = DateTime.Now;
                    request.DateUpdated = DateTime.Now;
                    request.Completed = false;
                }
                // If the none of the port request completion criteria have been met.
                else
                {
                    request.RequestStatus = numberStatuses?.FirstOrDefault();
                    request.DateUpdated = DateTime.Now;
                    request.Completed = false;
                }

                // Update the request in the database.
                var checkUpdate = await request.PutAsync(postgresSQL).ConfigureAwait(false);
                Log.Information($"[BulkVS] [PortRequests] Updated BulkVS Port Request {request?.TeliId} - {request?.RequestStatus} - {request?.DateCompleted?.ToShortDateString()}");

                // Get the original order and the numbers associated with the outstanding Port Request.
                var originalOrder = await Order.GetByIdAsync(request.OrderId, postgresSQL).ConfigureAwait(false);

                var notificationEmail = new Email
                {
                    PrimaryEmailAddress = originalOrder?.Email,
                    SalesEmailAddress = string.IsNullOrWhiteSpace(originalOrder?.SalesEmail) ? string.Empty : originalOrder.SalesEmail,
                    CarbonCopy = emailOrders,
                    OrderId = originalOrder.OrderId
                };

                string formattedNumbers = string.Empty;

                // If the port has just completed send out a notification email.
                if (portCompleted)
                {
                    // If the ported number haven't already been formatted for inclusion in the email do it now.
                    foreach (var ported in portedNumbers)
                    {
                        formattedNumbers += $"<br />{ported?.PortedDialedNumber} - {ported?.DateFirmOrderCommitment?.ToShortDateString()}";
                    }

                    // Port date set or updated.
                    notificationEmail.Subject = $"Your phone number has switched to Accelerate Networks successfully!";
                    notificationEmail.SalesEmailAddress = string.IsNullOrWhiteSpace(originalOrder.SalesEmail) ? string.Empty : originalOrder.SalesEmail;
                    notificationEmail.MessageBody = $@"Hi {originalOrder.FirstName},
<br />
<br />                                                                            
Great news, your old provider has released your phone numbers to Accelerate Networks!
<br />
<br />
The port request for the numbers listed below has been set to {portedNumbers?.FirstOrDefault()?.DateFirmOrderCommitment?.ToShortDateString()}, port requests usually complete at 9 AM PDT on the day of port completion.
<br />
<br />
Feel free to <a href='https://acceleratenetworks.com/Cart/Order/{originalOrder.OrderId}'>review the order here</a>, and let us know if you have any questions. It is now safe to cancel phone service with your old provider for the numbers that have ported in the list below.
<br />
<br />   
Numbers tied to this port request:
{formattedNumbers}
<br />
<br />
Sincerely,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)";

                    var checkSend = await notificationEmail.SendEmailAsync(smtpUsername, smtpPassword).ConfigureAwait(false);
                    var checkSave = await notificationEmail.PostAsync(postgresSQL).ConfigureAwait(false);

                    if (checkSend && checkSave)
                    {
                        Log.Information($"Sucessfully sent out the port completed email for Order {originalOrder.OrderId}.");
                    }
                    else
                    {
                        Log.Fatal($"Failed to sent out the port completed email for Order {originalOrder.OrderId}.");
                    }
                }
                else if (focChanged)
                {
                    foreach (var ported in portedNumbers)
                    {
                        formattedNumbers += $"<br />{ported?.PortedDialedNumber} - {ported?.DateFirmOrderCommitment?.ToShortDateString()}";
                    }

                    // Port date set or updated.
                    var hasFOCdate = portedNumbers?.Where(x => x.DateFirmOrderCommitment != null).FirstOrDefault();
                    notificationEmail.Subject = $"Port completion date set for {hasFOCdate?.DateFirmOrderCommitment}";
                    notificationEmail.SalesEmailAddress = string.IsNullOrWhiteSpace(originalOrder.SalesEmail) ? string.Empty : originalOrder.SalesEmail;
                    notificationEmail.MessageBody = $@"Hi {originalOrder.FirstName},
<br />
<br />                                                                            
Good news, your old provider is going to release your phone numbers to Accelerate Networks on {hasFOCdate?.DateFirmOrderCommitment?.ToShortDateString()}!
<br />
<br />    
Feel free to <a href='https://acceleratenetworks.com/Cart/Order/{originalOrder.OrderId}'>review the order here</a>, and let us know if you have any questions.
<br />
<br />   
Numbers porting to Accelerate Networks:
{formattedNumbers}
<br />
<br />
Sincerely,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)";

                    var checkSend = await notificationEmail.SendEmailAsync(smtpUsername, smtpPassword).ConfigureAwait(false);
                    var checkSave = await notificationEmail.PostAsync(postgresSQL).ConfigureAwait(false);

                    if (checkSend && checkSave)
                    {
                        Log.Information($"Sucessfully sent out the port date set email for Order {originalOrder.OrderId}.");
                    }
                    else
                    {
                        Log.Fatal($"Failed to sent out the port date set email for Order {originalOrder.OrderId}.");
                    }
                }

                Log.Information("[BulkVS] Completed the port request update process.");
            }
        }

        public async static Task UpdateStatusesTeliMessageAsync(IConfiguration configuration)
        {
            Log.Information("[TeliMessage] [PortRequests] Ingesting Port Request statuses.");

            var teleToken = Guid.Parse(configuration.GetConnectionString("TeleAPI"));
            var postgresSQL = configuration.GetConnectionString("PostgresqlProd");
            var smtpUsername = configuration.GetConnectionString("SmtpUsername");
            var smtpPassword = configuration.GetConnectionString("SmtpPassword");
            var emailOrders = configuration.GetConnectionString("EmailOrders");

            var portRequests = await PortRequest.GetAllAsync(postgresSQL).ConfigureAwait(false);

            foreach (var request in portRequests.Where(x => (x.VendorSubmittedTo == "TeliMessage" && x.Completed is false && x.DateSubmitted > DateTime.Now.AddYears(-3))).ToArray())
            {
                var teliStatus = await LnpGet.GetAsync(request?.TeliId, teleToken).ConfigureAwait(false);

                // If the request is found in Teli's database.
                if (teliStatus is not null && teliStatus?.code is 200)
                {
                    // All of the statuses for all of the numbers.
                    var numberStatuses = teliStatus?.data?.numbers_data?.Select(x => x.request_status);

                    var canceled = numberStatuses?.Where(x => x == "canceled");
                    var rejected = numberStatuses?.Where(x => x == "rejected");
                    var completed = numberStatuses?.Where(x => x == "completed");

                    // If all the numbers have been ported.
                    if ((completed != null) && (completed.Any()) && (completed.Count() == numberStatuses.Count()))
                    {
                        request.RequestStatus = "completed";
                        request.DateCompleted = DateTime.Now;
                        request.DateUpdated = DateTime.Now;
                        request.Completed = true;
                    }
                    // If the porting of a number has been canceled.
                    else if ((canceled != null) && (canceled.Any()))
                    {
                        request.RequestStatus = "canceled";
                        request.DateCompleted = DateTime.Now;
                        request.DateUpdated = DateTime.Now;
                        request.Completed = false;
                    }
                    // If a request to port a number has been rejected.
                    else if ((rejected != null) && (rejected.Any()))
                    {
                        request.RequestStatus = "rejected";
                        request.DateCompleted = DateTime.Now;
                        request.DateUpdated = DateTime.Now;
                        request.Completed = false;
                    }
                    // If the none of the port request completion criteria have been met.
                    else
                    {
                        request.RequestStatus = numberStatuses?.FirstOrDefault();
                        request.DateUpdated = DateTime.Now;
                        request.Completed = false;
                    }

                    // Update the request in the database.
                    var checkUpdate = await request.PutAsync(postgresSQL).ConfigureAwait(false);
                    Log.Information($"[TeliMessage] [PortRequests] Updated Teli Port Request {request?.TeliId} - {request?.RequestStatus} - {request?.DateCompleted?.ToShortDateString()}");

                    // Get the original order and the numbers associated with the outstanding Port Request.
                    var originalOrder = await Order.GetByIdAsync(request.OrderId, postgresSQL).ConfigureAwait(false);
                    var portedNumbers = await PortedPhoneNumber.GetByOrderIdAsync(request.OrderId, postgresSQL).ConfigureAwait(false);

                    var notificationEmail = new Email
                    {
                        PrimaryEmailAddress = originalOrder?.Email,
                        SalesEmailAddress = string.IsNullOrWhiteSpace(originalOrder?.SalesEmail) ? string.Empty : originalOrder.SalesEmail,
                        CarbonCopy = emailOrders,
                        OrderId = originalOrder.OrderId
                    };

                    bool focChanged = false;
                    bool portCompleted = false;

                    if (teliStatus?.data?.numbers_data != null && teliStatus.data.numbers_data.Any())
                    {
                        // Update the status of all of the numbers in the request.
                        foreach (var number in teliStatus?.data?.numbers_data)
                        {
                            var match = portedNumbers?.Where(x => x?.PortedDialedNumber == number?.number.Trim()).FirstOrDefault();

                            if (match != null)
                            {
                                // Update the request status if it has changed.
                                if (!string.IsNullOrWhiteSpace(number?.request_status) && (number?.request_status != match.RequestStatus))
                                {
                                    match.RequestStatus = number?.request_status.Trim();
                                    if (match.RequestStatus == "completed")
                                    {
                                        portCompleted = true;
                                        match.Completed = true;
                                    }
                                }

                                // Update the FOC date if it has changed.
                                var checkFocDateParse = DateTime.TryParse(number?.foc_date, out var focDate);

                                if (checkFocDateParse && match.DateFirmOrderCommitment != focDate)
                                {
                                    match.DateFirmOrderCommitment = focDate;
                                    focChanged = true;
                                }

                                var checkPortedNumberUpdate = await match.PutAsync(postgresSQL).ConfigureAwait(false);
                                Log.Information($"[TeleMessage] [PortRequests] Updated Teli Port Request {request?.TeliId} - {match?.PortedDialedNumber} - {match?.RequestStatus} - {match?.DateFirmOrderCommitment?.ToShortDateString()}");
                            }
                            else
                            {
                                // If the number isn't already assocaited with the Port request add it to the list of Ported Numbers.
                                bool checkNpa = int.TryParse(number?.number?.Substring(0, 3), out int npa);
                                bool checkNxx = int.TryParse(number?.number?.Substring(3, 3), out int nxx);
                                bool checkXxxx = int.TryParse(number?.number?.Substring(6, 4), out int xxxx);

                                var supriseNumber = new PortedPhoneNumber
                                {
                                    PortedDialedNumber = number?.number,
                                    NPA = npa,
                                    NXX = nxx,
                                    XXXX = xxxx,
                                    OrderId = originalOrder?.OrderId,
                                    PortRequestId = request?.PortRequestId,
                                    RequestStatus = number?.request_status.Trim()
                                };

                                var checkFocDateParse = DateTime.TryParse(number?.foc_date, out var focDate);

                                if (checkFocDateParse)
                                {
                                    supriseNumber.DateFirmOrderCommitment = focDate;
                                }

                                var checkInsertPortedNumber = await supriseNumber.PostAsync(postgresSQL).ConfigureAwait(false);
                                Log.Information($"[TeleMessage] [PortRequests] Updated Teli Port Request with new phone number {request?.TeliId} - {match?.PortedDialedNumber} - {match?.RequestStatus} - {match?.DateFirmOrderCommitment?.ToShortDateString()}");
                            }
                        }
                    }

                    string formattedNumbers = string.Empty;

                    // If the port has just completed send out a notification email.
                    if (portCompleted)
                    {
                        // If the ported number haven't already been formatted for inclusion in the email do it now.
                        foreach (var ported in portedNumbers)
                        {
                            formattedNumbers += $"<br />{ported?.PortedDialedNumber} - {ported?.DateFirmOrderCommitment?.ToShortDateString()}";
                        }

                        // Port date set or updated.
                        notificationEmail.Subject = $"Your phone number has switched to Accelerate Networks successfully!";
                        notificationEmail.SalesEmailAddress = string.IsNullOrWhiteSpace(originalOrder.SalesEmail) ? string.Empty : originalOrder.SalesEmail;
                        notificationEmail.MessageBody = $@"Hi {originalOrder.FirstName},
<br />
<br />                                                                            
Great news, your old provider has released your phone numbers to Accelerate Networks!
<br />
<br />
The port request for the numbers listed below has been set to {portedNumbers?.FirstOrDefault()?.DateFirmOrderCommitment?.ToShortDateString()}, port requests usually complete at 9 AM PDT on the day of port completion.
<br />
<br />
Feel free to <a href='https://acceleratenetworks.com/Cart/Order/{originalOrder.OrderId}'>review the order here</a>, and let us know if you have any questions. It is now safe to cancel phone service with your old provider for the numbers that have ported in the list below.
<br />
<br />   
Numbers tied to this port request:
{formattedNumbers}
<br />
<br />
Sincerely,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)";

                        var checkSend = await notificationEmail.SendEmailAsync(smtpUsername, smtpPassword).ConfigureAwait(false);
                        var checkSave = await notificationEmail.PostAsync(postgresSQL).ConfigureAwait(false);

                        if (checkSend && checkSave)
                        {
                            Log.Information($"Sucessfully sent out the port date set email for Order {originalOrder.OrderId}.");
                        }
                        else
                        {
                            Log.Fatal($"Failed to sent out the port date set email for Order {originalOrder.OrderId}.");
                        }
                    }
                    else if (focChanged)
                    {
                        foreach (var ported in portedNumbers)
                        {
                            formattedNumbers += $"<br />{ported?.PortedDialedNumber} - {ported?.DateFirmOrderCommitment?.ToShortDateString()}";
                        }

                        // Port date set or updated.
                        var hasFOCdate = portedNumbers?.Where(x => x.DateFirmOrderCommitment != null).FirstOrDefault();
                        notificationEmail.Subject = $"Port completion date set for {hasFOCdate?.DateFirmOrderCommitment}";
                        notificationEmail.SalesEmailAddress = string.IsNullOrWhiteSpace(originalOrder.SalesEmail) ? string.Empty : originalOrder.SalesEmail;
                        notificationEmail.MessageBody = $@"Hi {originalOrder.FirstName},
<br />
<br />                                                                            
Good news, your old provider is going to release your phone numbers to Accelerate Networks on {hasFOCdate?.DateFirmOrderCommitment?.ToShortDateString()}!
<br />
<br />    
Feel free to <a href='https://acceleratenetworks.com/Cart/Order/{originalOrder.OrderId}'>review the order here</a>, and let us know if you have any questions.
<br />
<br />   
Numbers porting to Accelerate Networks:
{formattedNumbers}
<br />
<br />
Sincerely,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)";

                        var checkSend = await notificationEmail.SendEmailAsync(smtpUsername, smtpPassword).ConfigureAwait(false);
                        var checkSave = await notificationEmail.PostAsync(postgresSQL).ConfigureAwait(false);

                        if (checkSend && checkSave)
                        {
                            Log.Information($"Sucessfully sent out the port date set email for Order {originalOrder.OrderId}.");
                        }
                        else
                        {
                            Log.Fatal($"Failed to sent out the port date set email for Order {originalOrder.OrderId}.");
                        }
                    }
                }
                else
                {
                    Log.Fatal($"[TeliMessage] [PortRequests] Failed to retrive Port Request for Order {request.OrderId} in Teli's API.");
                    Log.Fatal($"[TeliMessage] [PortRequests] {JsonSerializer.Serialize(teliStatus)}");
                }
            }

            Log.Information("[TeliMessage] [PortRequests] Completed the port request update process.");
        }
    }
}