using FirstCom;

using Flurl.Http;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;

using PhoneNumbersNA;

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
    public class Owned
    {
        public async static Task IngestAsync(IngestConfiguration configuration)
        {
            Log.Information("[OwnedNumbers] Ingesting data for OwnedNumbers.");
            var allNumbers = new List<OwnedPhoneNumber>();
            var start = DateTime.Now;

            // Ingest all owned numbers from the providers.
            try
            {
                var firstComNumbers = await FirstPointComAsync(configuration.PComNetUsername, configuration.PComNetPassword).ConfigureAwait(false);
                if (firstComNumbers != null)
                {
                    allNumbers.AddRange(firstComNumbers);
                };

                var bulkVSNumbers = await TnRecord.GetOwnedAsync(configuration.BulkVSUsername, configuration.BulkVSPassword).ConfigureAwait(false);
                if (bulkVSNumbers != null)
                {
                    allNumbers.AddRange(bulkVSNumbers);
                };
            }
            catch (Exception ex)
            {
                Log.Fatal("[OwnedNumbers] Failed to retrive owned numbers.");
                Log.Fatal(ex.Message);
                Log.Fatal(ex?.StackTrace ?? "No stacktrace found.");
            }

            // If we ingested any owned numbers update the database.
            IngestStatistics ownedNumberStats;
            if (allNumbers.Count > 0)
            {
                Log.Information($"[OwnedNumbers] Submitting {allNumbers.Count} numbers to the database.");
                ownedNumberStats = await SubmitOwnedNumbersAsync(allNumbers, configuration.Postgresql).ConfigureAwait(false);
            }
            else
            {
                Log.Fatal("[OwnedNumbers] No ownend numbers ingested. Skipping submission to the database.");
                ownedNumberStats = new IngestStatistics
                {
                    StartDate = start,
                    EndDate = DateTime.Now,
                    FailedToIngest = 0,
                    IngestedNew = 0,
                    Lock = false,
                    NumbersRetrived = 0,
                    IngestedFrom = "OwnedNumbers",
                    Priority = false,
                    Removed = 0,
                    Unchanged = 0,
                    UpdatedExisting = 0
                };
            }

            // Look for LRN changes.
            try
            {
                Log.Information("[OwnedNumbers] Looking for LRN changes on owned numbers.");
                var changedNumbers = await VerifyServiceProvidersAsync(configuration.BulkVSAPIKEY, configuration.Postgresql).ConfigureAwait(false);

                if (changedNumbers != null && changedNumbers.Any())
                {
                    Log.Information($"[OwnedNumbers] Emailing out a notification that {changedNumbers.Count()} numbers LRN updates.");
                    var checkSend = await SendPortingNotificationEmailAsync(changedNumbers, configuration.SmtpUsername, configuration.SmtpPassword, configuration.EmailDan, configuration.EmailOrders, configuration.Postgresql).ConfigureAwait(false);
                }

                var orderStatuses = await Orders.IncompleteOrderRemindersAsync(configuration.Postgresql).ConfigureAwait(false);

                if (orderStatuses != null && orderStatuses.Any())
                {
                    Log.Information($"[OwnedNumbers] Emailing out a notification for {orderStatuses.Count()} incomplete orders.");
                    var checkSend = await Orders.SendOrderReminderEmailAsync(orderStatuses, configuration.SmtpUsername, configuration.SmtpPassword, configuration.EmailDan, configuration.EmailOrders, configuration.Postgresql).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal("[OwnedNumbers] Failed to look for LRN changes on owned numbers.");
                Log.Fatal(ex.Message);
            }

            // Offer unassigned phone numbers we own for purchase on the website.
            _ = await OfferUnassignedNumberForSaleAsync(configuration.Postgresql).ConfigureAwait(false);

            // Match up owned numbers and their billingClients.
            _ = await MatchOwnedNumbersToBillingClientsAsync(configuration.Postgresql).ConfigureAwait(false);

            // Remove the lock from the database to prevent it from getting cluttered with blank entries.
            var lockEntry = await IngestStatistics.GetLockAsync("OwnedNumbers", configuration.Postgresql).ConfigureAwait(false);
            _ = await lockEntry.DeleteAsync(configuration.Postgresql).ConfigureAwait(false);

            // Remove all of the old numbers from the database.
            Log.Information("[OwnedNumbers] Marking numbers that failed to reingest as inactive in the database.");

            var combined = new IngestStatistics
            {
                StartDate = start,
                EndDate = DateTime.Now,
                FailedToIngest = ownedNumberStats.FailedToIngest,
                IngestedFrom = ownedNumberStats.IngestedFrom,
                IngestedNew = ownedNumberStats.IngestedNew,
                Lock = false,
                NumbersRetrived = ownedNumberStats.NumbersRetrived,
                Removed = 0,
                Unchanged = ownedNumberStats.Unchanged,
                UpdatedExisting = ownedNumberStats.UpdatedExisting,
                Priority = false
            };

            if (await combined.PostAsync(configuration.Postgresql).ConfigureAwait(false))
            {
                Log.Information("[OwnedNumbers] Completed the ingest process.");
            }
            else
            {
                Log.Fatal("[OwnedNumbers] Failed to completed the ingest process.");
            }
        }

        public static async Task<IEnumerable<OwnedPhoneNumber>> FirstPointComAsync(string username, string password)
        {
            var numbers = new List<OwnedPhoneNumber>();

            foreach (var npa in PhoneNumbersNA.AreaCode.All)
            {
                try
                {
                    var results = await FirstPointComOwnedPhoneNumber.GetAsync(npa.ToString(), username, password).ConfigureAwait(false);

                    Log.Information($"[OwnedNumbers] [FirstPointCom] Retrived {results.DIDOrder.Length} owned numbers.");

                    foreach (var item in results.DIDOrder)
                    {
                        var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.DID, out var phoneNumber);

                        if (checkParse && phoneNumber is not null)
                        {
                            numbers.Add(new OwnedPhoneNumber
                            {
                                DialedNumber = phoneNumber.DialedNumber ?? string.Empty,
                                IngestedFrom = "FirstPointCom",
                                Active = true,
                                DateIngested = DateTime.Now
                            });
                        }
                        else
                        {
                            Log.Fatal($"[OwnedNumber] Failed to parse Owned Number {item.DID} from FirstPointCom.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    Log.Error($"[OwnedNumbers] [FirstPointCom] No numbers found for NPA {npa}");
                }
            }

            Log.Information($"[OwnedNumbers] [FirstPointCom] Ingested {numbers.Count} owned numbers.");

            return numbers.Any() ? numbers.ToArray() : new List<OwnedPhoneNumber>();
        }

        public static async Task<IngestStatistics> SubmitOwnedNumbersAsync(IEnumerable<OwnedPhoneNumber> numbers, string connectionString)
        {
            var start = DateTime.Now;
            var ingestedNew = 0;
            var updatedExisting = 0;

            var existingOwnedNumbers = await OwnedPhoneNumber.GetAllAsync(connectionString).ConfigureAwait(false);

            foreach (var item in numbers)
            {
                // TODO: Convert this to a dictionary lookup.
                var number = existingOwnedNumbers.Where(x => x.DialedNumber == item.DialedNumber).FirstOrDefault();

                if (number is null)
                {
                    var checkCreate = await item.PostAsync(connectionString).ConfigureAwait(false);
                    if (checkCreate)
                    {
                        ingestedNew++;
                        Log.Information($"[OwnedNumbers] Added {item.DialedNumber} as an owned number.");
                    }
                    else
                    {
                        Log.Fatal($"[OwnedNumbers] Failed to add {item.DialedNumber} as an owned number.");
                    }
                }
                else
                {
                    item.Notes = string.IsNullOrWhiteSpace(number.Notes) ? item.Notes : number.Notes;

                    var checkCreate = await item.PutAsync(connectionString).ConfigureAwait(false);
                    if (checkCreate)
                    {
                        updatedExisting++;
                        Log.Information($"[OwnedNumbers] Updated {item.DialedNumber} as an owned number.");
                    }
                    else
                    {
                        Log.Fatal($"[OwnedNumbers] Failed to update {item.DialedNumber} as an owned number.");
                    }
                }
            }

            var end = DateTime.Now;

            var stats = new IngestStatistics
            {
                StartDate = start,
                EndDate = end,
                IngestedFrom = "OwnedNumbers",
                NumbersRetrived = numbers.Count(),
                Priority = false,
                Lock = false,
                IngestedNew = ingestedNew,
                UpdatedExisting = updatedExisting,
                Removed = 0,
                Unchanged = 0,
                FailedToIngest = 0
            };

            Log.Information($"[OwnedNumbers] Updated {updatedExisting} owned numbers.");
            Log.Information($"[OwnedNumbers] Added {ingestedNew} new owned numbers.");

            return stats;
        }

        public static async Task<IngestStatistics> OfferUnassignedNumberForSaleAsync(string connectionString)
        {
            var start = DateTime.Now;
            var ingestedNew = 0;
            var updatedExisting = 0;

            var numbers = await OwnedPhoneNumber.GetAllAsync(connectionString).ConfigureAwait(false);

            var newUnassigned = new List<DataAccess.PhoneNumber>();

            foreach (var item in numbers)
            {
                if (item?.Notes is not null && item?.Notes.Trim() == "Unassigned")
                {
                    var number = await DataAccess.PhoneNumber.GetAsync(item.DialedNumber, connectionString).ConfigureAwait(false);

                    if (number is null)
                    {
                        var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(number?.DialedNumber ?? string.Empty, out var phoneNumber);

                        if (checkParse && phoneNumber is not null)
                        {
                            number = new DataAccess.PhoneNumber
                            {
                                NPA = phoneNumber.NPA,
                                NXX = phoneNumber.NXX,
                                XXXX = phoneNumber.XXXX,
                                DialedNumber = phoneNumber.DialedNumber ?? string.Empty,
                                DateIngested = item.DateIngested,
                                IngestedFrom = "OwnedNumber"
                            };

                            newUnassigned.Add(number);
                            ingestedNew++;

                            Log.Information($"[Ingest] [OwnedNumber] Put unassigned number {item.DialedNumber} up for sale.");
                        }
                        else
                        {
                            Log.Fatal($"[Ingest] [OwnedNumber] Failed to put unassigned number {item.DialedNumber} up for sale. Number could not be parsed.");
                        }
                    }
                    else
                    {
                        number.DateIngested = item.DateIngested;
                        number.IngestedFrom = "OwnedNumber";

                        var checkCreate = await number.PutAsync(connectionString).ConfigureAwait(false);
                        updatedExisting++;

                        Log.Information($"[Ingest] [OwnedNumber] Continued offering unassigned number {item.DialedNumber} up for sale.");
                    }
                }
            }

            var typedNumbers = Services.AssignNumberTypes(newUnassigned).ToArray();
            var locations = await Services.AssignRatecenterAndRegionAsync(typedNumbers).ConfigureAwait(false);
            _ = await Services.SubmitPhoneNumbersAsync(locations.ToArray(), connectionString).ConfigureAwait(false);

            var end = DateTime.Now;

            var stats = new IngestStatistics
            {
                StartDate = start,
                EndDate = end,
                IngestedFrom = "OwnedNumbers",
                NumbersRetrived = numbers.Count(),
                Priority = false,
                Lock = false,
                IngestedNew = ingestedNew,
                UpdatedExisting = updatedExisting,
                Removed = 0,
                Unchanged = 0,
                FailedToIngest = 0
            };

            return stats;
        }

        public static async Task<IngestStatistics> MatchOwnedNumbersToBillingClientsAsync(string connectionString)
        {
            var start = DateTime.Now;
            var updatedExisting = 0;

            var numbers = await OwnedPhoneNumber.GetAllAsync(connectionString).ConfigureAwait(false);
            var purcahsed = await PurchasedPhoneNumber.GetAllAsync(connectionString).ConfigureAwait(false);
            var ported = await PortedPhoneNumber.GetAllAsync(connectionString).ConfigureAwait(false);

            foreach (var number in numbers.Where(x => string.IsNullOrWhiteSpace(x.BillingClientId)))
            {
                var match = purcahsed.Where(x => x.DialedNumber == number.DialedNumber).FirstOrDefault();

                if (match is null)
                {
                    var match2 = ported.Where(x => x.PortedDialedNumber == number.DialedNumber).FirstOrDefault();

                    if (match2 is null)
                    {
                        // Do nothing if we can't find a match in our system for this number.
                        Log.Information($"[OwnedNumbers] [ClientMatch] Couldn't associate Owned Number {number.DialedNumber} with a billing client.");
                        continue;
                    }

                    var order = await Order.GetByIdAsync(match2.OrderId ?? Guid.NewGuid(), connectionString).ConfigureAwait(false);

                    if (order is not null && !string.IsNullOrWhiteSpace(order?.BillingClientId))
                    {
                        number.BillingClientId = order.BillingClientId;
                        number.OwnedBy = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName;

                        var checkUpdate = await number.PutAsync(connectionString).ConfigureAwait(false);
                        updatedExisting++;

                        Log.Information($"[OwnedNumbers] [ClientMatch] Associated Owned Number {match2?.PortedDialedNumber} with billing client id {order?.BillingClientId}");
                    }
                    else
                    {
                        Log.Information($"[OwnedNumbers] [ClientMatch] Couldn't associate Owned Number {match2?.PortedDialedNumber} with a billing client.");
                    }
                }
                else
                {
                    var order = await Order.GetByIdAsync(match.OrderId, connectionString).ConfigureAwait(false);

                    if (order is not null && !string.IsNullOrWhiteSpace(order?.BillingClientId))
                    {
                        number.BillingClientId = order.BillingClientId;
                        number.OwnedBy = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName;

                        var checkUpdate = await number.PutAsync(connectionString).ConfigureAwait(false);
                        updatedExisting++;

                        Log.Information($"[OwnedNumbers] [ClientMatch] Associated Owned Number {match?.DialedNumber} with billing client id {order?.BillingClientId}");
                    }
                    else
                    {
                        Log.Information($"[OwnedNumbers] [ClientMatch] Couldn't associate Owned Number {match?.DialedNumber} with a billing client.");
                    }
                }
            }

            var end = DateTime.Now;

            var stats = new IngestStatistics
            {
                StartDate = start,
                EndDate = end,
                IngestedFrom = "OwnedNumbers",
                NumbersRetrived = numbers.Count(),
                Priority = false,
                Lock = false,
                IngestedNew = 0,
                UpdatedExisting = updatedExisting,
                Removed = 0,
                Unchanged = 0,
                FailedToIngest = 0
            };

            return stats;
        }


        public class ServiceProviderChanged
        {
            public string DialedNumber { get; set; } = string.Empty;
            public string OldSPID { get; set; } = string.Empty;
            public string CurrentSPID { get; set; } = string.Empty;
            public string OldSPIDName { get; set; } = string.Empty;
            public string CurrentSPIDName { get; set; } = string.Empty;
        }

        public static async Task<IEnumerable<ServiceProviderChanged>> VerifyServiceProvidersAsync(string bulkApiKey, string connectionString)
        {
            var owned = await OwnedPhoneNumber.GetAllAsync(connectionString).ConfigureAwait(false);
            var serviceProviderChanged = new List<ServiceProviderChanged>();

            foreach (var number in owned)
            {
                var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(number.DialedNumber, out var phoneNumber);

                if (checkParse && phoneNumber is not null && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
                {
                    if (phoneNumber.DialedNumber.IsTollfree())
                    {
                        // Skip the LRNlookup if the current number is TollFree.
                        Log.Information($"[OwnedNumbers] Skipping Toll Free number {number.DialedNumber}.");
                    }
                    else
                    {
                        var result = await LrnBulkCnam.GetAsync(number.DialedNumber, bulkApiKey).ConfigureAwait(false);

                        var provider = "BulkVS";
                        var newSpid = result?.spid ?? string.Empty;
                        var newSpidName = result?.lec ?? string.Empty;

                        var updatedSPID = newSpid != number.SPID;
                        var updatedSPIDName = newSpidName != number.SPIDName;

                        if (updatedSPID || updatedSPIDName)
                        {
                            serviceProviderChanged.Add(new ServiceProviderChanged
                            {
                                CurrentSPID = newSpid,
                                OldSPID = number.SPID,
                                CurrentSPIDName = newSpidName,
                                OldSPIDName = number.SPIDName,
                                DialedNumber = number.DialedNumber
                            });

                            // Update the SPID to the current value.
                            number.SPID = newSpid;
                            number.SPIDName = newSpidName;
                            var checkUpdate = await number.PutAsync(connectionString).ConfigureAwait(false);
                            if (checkUpdate)
                            {
                                Log.Information($"[OwnedNumbers] Updated {newSpidName}, {newSpid} for {number.DialedNumber} from [{provider}].");
                            }
                            else
                            {
                                Log.Fatal($"[OwnedNumbers] Failed to update {newSpidName}, {newSpid} for {number.DialedNumber} from [{provider}].");
                            }
                        }
                        else
                        {
                            Log.Information($"[OwnedNumbers] Found {newSpidName}, {newSpid} for {number.DialedNumber} from [{provider}].");
                        }
                    }
                }
                else
                {
                    Log.Fatal($"[OwnedNumbers] Failed to parsed Owned Number {number.DialedNumber}.");
                }
            }

            Log.Information($"[OwnedNumbers] Found {serviceProviderChanged.Count} numbers whose Service Provider has changed since the last ingest.");

            return serviceProviderChanged;
        }

        public static async Task<IEnumerable<OwnedPhoneNumber>> VerifyEmergencyInformationAsync(IEnumerable<OwnedPhoneNumber> owned, string connectionString)
        {
            Log.Information($"[OwnedNumbers] Verifying Emergency Information for {owned?.Count()} Owned Phone numbers.");

            var emergencyInformation = await EmergencyInformation.GetAllAsync(connectionString).ConfigureAwait(false);
            // TODO

            return owned ?? new List<OwnedPhoneNumber>();
        }

        public static async Task<bool> SendPortingNotificationEmailAsync(IEnumerable<ServiceProviderChanged> changes, string smtpUsername, string smtpPassword, string emailPrimary, string emailCC, string connectionString)
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

            var notificationEmail = new Email
            {
                PrimaryEmailAddress = emailPrimary,
                CarbonCopy = emailCC,
                DateSent = DateTime.Now,
                Subject = $"[Ingest] {changes.Count()} phone numbers changed Service Providers.",
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