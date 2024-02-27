using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;

using Serilog;

using System;
using System.Linq;
using System.Threading.Tasks;

using static NumberSearch.Ingest.Program;

namespace NumberSearch.Ingest
{
    public class PortRequests
    {
        public async static Task UpdateStatusesBulkVSAsync(IngestConfiguration configuration)
        {
            Log.Information("[BulkVS] [PortRequests] Ingesting Port Request statuses.");

            var bulkVSPortRequests = await PortTn.GetAllAsync(configuration.BulkVSUsername, configuration.BulkVSPassword).ConfigureAwait(false);

            foreach (var request in bulkVSPortRequests.ToArray())
            {
                var portedNumbers = await PortedPhoneNumber.GetByExternalIdAsync(request.OrderId, configuration.Postgresql).ConfigureAwait(false);

                bool focChanged = false;
                bool portCompleted = false;

                var bulkStatus = await PortTn.GetAsync(request.OrderId, configuration.BulkVSUsername, configuration.BulkVSPassword).ConfigureAwait(false);

                foreach (var number in portedNumbers)
                {
                    // If the request has been assigned and external port request id than it has been submitted to the vendor.
                    if (!string.IsNullOrWhiteSpace(number.ExternalPortRequestId) && bulkStatus is not null)
                    {
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

                            var checkPortedNumberUpdate = await number.PutAsync(configuration.Postgresql).ConfigureAwait(false);
                            Log.Information($"[BulkVS] [PortRequests] Updated BulkVS Port Request {request?.OrderId} - {number?.PortedDialedNumber} - {number?.RequestStatus} - {number?.DateFirmOrderCommitment?.ToShortDateString()}");
                        }
                    }
                }

                // All of the statuses for all of the numbers.
                var numberStatuses = portedNumbers.Select(x => x.RequestStatus);

                var canceled = numberStatuses?.Where(x => x == "CANCELLED");
                var rejected = numberStatuses?.Where(x => x == "EXCEPTION");
                var completed = numberStatuses?.Where(x => x == "COMPLETE");

                if (portedNumbers.FirstOrDefault() is not null)
                {
                    var portRequest = await PortRequest.GetByOrderIdAsync(portedNumbers.FirstOrDefault()?.OrderId ?? Guid.Empty, configuration.Postgresql).ConfigureAwait(false);

                    if (portRequest is not null && portRequest.OrderId == portedNumbers.FirstOrDefault()?.OrderId)
                    {
                        // If all the numbers have been ported.
                        if ((completed != null) && completed.Any() && numberStatuses is not null && completed.Count() == numberStatuses.Count())
                        {
                            portRequest.RequestStatus = "COMPLETE";
                            portRequest.DateCompleted = DateTime.Now;
                            portRequest.DateUpdated = DateTime.Now;
                            portRequest.Completed = true;
                            portRequest.VendorSubmittedTo = "BulkVS";
                        }
                        // If the porting of a number has been canceled.
                        else if ((canceled != null) && (canceled.Any()))
                        {
                            portRequest.RequestStatus = "CANCELLED";
                            portRequest.DateCompleted = DateTime.Now;
                            portRequest.DateUpdated = DateTime.Now;
                            portRequest.Completed = false;
                            portRequest.VendorSubmittedTo = "BulkVS";
                        }
                        // If a request to port a number has been rejected.
                        else if ((rejected != null) && (rejected.Any()))
                        {
                            portRequest.RequestStatus = "EXCEPTION";
                            portRequest.DateCompleted = DateTime.Now;
                            portRequest.DateUpdated = DateTime.Now;
                            portRequest.Completed = false;
                            portRequest.VendorSubmittedTo = "BulkVS";
                        }
                        // If the none of the port request completion criteria have been met.
                        else
                        {
                            portRequest.RequestStatus = numberStatuses?.FirstOrDefault() ?? string.Empty;
                            portRequest.DateUpdated = DateTime.Now;
                            portRequest.Completed = false;
                            portRequest.VendorSubmittedTo = "BulkVS";
                        }

                        // Update the request in the database.
                        var checkUpdate = await portRequest.PutAsync(configuration.Postgresql).ConfigureAwait(false);
                        Log.Information($"[BulkVS] [PortRequests] Updated BulkVS Port Request {portRequest?.BulkVSId} - {portRequest?.RequestStatus} - {portRequest?.DateCompleted?.ToShortDateString()}");

                        // Get the original order and the numbers associated with the outstanding Port Request.
                        var originalOrder = await Order.GetByIdAsync(portRequest!.OrderId, configuration.Postgresql).ConfigureAwait(false);

                        if (originalOrder is not null)
                        {
                            var notificationEmail = new Email
                            {
                                PrimaryEmailAddress = originalOrder.Email,
                                SalesEmailAddress = string.IsNullOrWhiteSpace(originalOrder?.SalesEmail) ? string.Empty : originalOrder.SalesEmail,
                                CarbonCopy = configuration.EmailOrders,
                                OrderId = originalOrder!.OrderId
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
The port request for the numbers listed below has been set to {portedNumbers?.FirstOrDefault()?.DateFirmOrderCommitment?.ToShortDateString()}, port requests usually complete at 5 PM PST on the day of port completion.
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

                                var checkSend = await notificationEmail.SendEmailAsync(configuration.SmtpUsername, configuration.SmtpPassword).ConfigureAwait(false);
                                var checkSave = await notificationEmail.PostAsync(configuration.Postgresql).ConfigureAwait(false);

                                if (checkSend && checkSave)
                                {
                                    Log.Information($"Successfully sent out the port completed email for Order {originalOrder.OrderId}.");
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

                                var checkSend = await notificationEmail.SendEmailAsync(configuration.SmtpUsername, configuration.SmtpPassword).ConfigureAwait(false);
                                var checkSave = await notificationEmail.PostAsync(configuration.Postgresql).ConfigureAwait(false);

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
                    }
                }

                Log.Information("[BulkVS] Completed the port request update process.");
            }
        }

        public async static Task UpdatePortRequestsAndNumbersByExternalIdAsync(IngestConfiguration configuration)
        {
            Log.Information("[BulkVS] [PortRequests] Ingesting Port Request statuses using the external OrderId.");

            var portRequests = await PortRequest.GetAllAsync(configuration.Postgresql);
            var notCompleted = portRequests.Where(x => x.VendorSubmittedTo is "BulkVS" && x.RequestStatus is not "COMPLETE" && x.DateCompleted is null && !string.IsNullOrWhiteSpace(x.BulkVSId)).ToArray();
            foreach (var request in notCompleted)
            {
                string[] OrderIds = request.BulkVSId.Replace(" ", "").Split(",");
                foreach (var orderId in OrderIds)
                {
                    PortTn bulkVSPortRequest = await PortTn.GetAsync(orderId, configuration.BulkVSUsername, configuration.BulkVSPassword);
                    var firstNumber = bulkVSPortRequest.TNList.FirstOrDefault();
                    if (bulkVSPortRequest.OrderDetails.OrderId == orderId && firstNumber is not null && !string.IsNullOrWhiteSpace(firstNumber.LNPStatus))
                    {
                        request.RequestStatus = firstNumber.LNPStatus;

                        var related = await PortedPhoneNumber.GetByPortRequestIdAsync(request.PortRequestId, configuration.Postgresql);
                        foreach (var port in related)
                        {
                            var match = bulkVSPortRequest.TNList.FirstOrDefault(x => x.TN.Contains(port.PortedDialedNumber));
                            if (match is not null && !string.IsNullOrWhiteSpace(match.LNPStatus) && port.RequestStatus != match.LNPStatus)
                            {
                                port.RequestStatus = match.LNPStatus;
                                port.ExternalPortRequestId = bulkVSPortRequest.OrderDetails.OrderId;
                                var checkRDDParse = DateTime.TryParse(match.RDD, out var FOCDate);
                                if (checkRDDParse)
                                {
                                    port.DateFirmOrderCommitment = FOCDate;
                                }
                                var checkUpdate = await port.PutAsync(configuration.Postgresql);
                            }
                        }
                    }
                    // If there is no first number then BulkVS returned a 404, which means the port request has been removed from their system and we won't be able to get updates on it.
                    if (firstNumber is null)
                    {
                        request.DateCompleted = DateTime.Now;
                        request.DateUpdated = DateTime.Now;
                        request.RequestStatus = "404";
                    }
                }
                var checkUpdated = await request.PutAsync(configuration.Postgresql);
            }

            var portedNumbers = await PortedPhoneNumber.GetAllAsync(configuration.Postgresql);
            var inComplete = portedNumbers.Where(x => x.RequestStatus is not "COMPLETE" && x.RequestStatus is not "404" && !string.IsNullOrWhiteSpace(x.ExternalPortRequestId)).ToArray();
            foreach (var number in inComplete)
            {
                PortTn bulkVSPortRequest = await PortTn.GetAsync(number.ExternalPortRequestId, configuration.BulkVSUsername, configuration.BulkVSPassword);
                var match = bulkVSPortRequest.TNList.FirstOrDefault(x => x.TN.Contains(number.PortedDialedNumber));
                if (bulkVSPortRequest.OrderDetails.OrderId == number.ExternalPortRequestId && match is not null && !string.IsNullOrWhiteSpace(match.LNPStatus) && number.RequestStatus != match.LNPStatus)
                {
                    number.RequestStatus = match.LNPStatus;
                    number.ExternalPortRequestId = bulkVSPortRequest.OrderDetails.OrderId;
                    var checkRDDParse = DateTime.TryParse(match.RDD, out var FOCDate);
                    if (checkRDDParse)
                    {
                        number.DateFirmOrderCommitment = FOCDate;
                    }
                    var checkUpdate = await number.PutAsync(configuration.Postgresql);
                }
                // If there is no first number then BulkVS returned a 404, which means the port request has been removed from their system and we won't be able to get updates on it.
                if (match is null)
                {
                    number.DateIngested = DateTime.Now;
                    number.RequestStatus = "404";
                    number.Completed = false;
                    var checkUpdate = await number.PutAsync(configuration.Postgresql);
                }
            }
        }
    }
}