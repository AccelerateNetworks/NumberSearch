using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;

using Serilog;

using System;
using System.Linq;
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

            var bulkVSPortRequests = await PortTn.GetAllAsync(bulkVSusername, bulkVSpassword).ConfigureAwait(false);

            foreach (var request in bulkVSPortRequests.ToArray())
            {
                var portedNumbers = await PortedPhoneNumber.GetByExternalIdAsync(request.OrderId, postgresSQL).ConfigureAwait(false);

                bool focChanged = false;
                bool portCompleted = false;

                var bulkStatus = await PortTn.GetAsync(request.OrderId, bulkVSusername, bulkVSpassword).ConfigureAwait(false);

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

                            var checkPortedNumberUpdate = await number.PutAsync(postgresSQL).ConfigureAwait(false);
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
                    var portRequest = await PortRequest.GetByOrderIdAsync(portedNumbers.FirstOrDefault()?.OrderId ?? Guid.Empty, postgresSQL).ConfigureAwait(false);

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
                            portRequest.RequestStatus = numberStatuses?.FirstOrDefault();
                            portRequest.DateUpdated = DateTime.Now;
                            portRequest.Completed = false;
                            portRequest.VendorSubmittedTo = "BulkVS";
                        }

                        // Update the request in the database.
                        var checkUpdate = await portRequest.PutAsync(postgresSQL).ConfigureAwait(false);
                        Log.Information($"[BulkVS] [PortRequests] Updated BulkVS Port Request {portRequest?.BulkVSId} - {portRequest?.RequestStatus} - {portRequest?.DateCompleted?.ToShortDateString()}");

                        // Get the original order and the numbers associated with the outstanding Port Request.
                        var originalOrder = await Order.GetByIdAsync(portRequest!.OrderId, postgresSQL).ConfigureAwait(false);

                        if (originalOrder is not null)
                        {
                            var notificationEmail = new Email
                            {
                                PrimaryEmailAddress = originalOrder?.Email,
                                SalesEmailAddress = string.IsNullOrWhiteSpace(originalOrder?.SalesEmail) ? string.Empty : originalOrder.SalesEmail,
                                CarbonCopy = emailOrders,
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
                        }
                    }
                }

                Log.Information("[BulkVS] Completed the port request update process.");
            }
        }
    }
}