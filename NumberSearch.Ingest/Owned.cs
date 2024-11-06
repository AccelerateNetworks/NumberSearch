using FirstCom.Models;

using Flurl.Http;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.FusionPBX;

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

        public static async Task OwnedDailyAsync(IngestConfiguration appConfig)
        {

            // Prevent another run from starting while this is still going.
            IngestStatistics lockingStats = new()
            {
                IngestedFrom = "OwnedNumbers",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now,
                IngestedNew = 0,
                FailedToIngest = 0,
                NumbersRetrived = 0,
                Removed = 0,
                Unchanged = 0,
                UpdatedExisting = 0,
                Lock = true
            };

            await lockingStats.PostAsync(appConfig.Postgresql.ToString());
            await Owned.IngestAsync(appConfig);

            // Remove the lock from the database to prevent it from getting cluttered with blank entries.
            await lockingStats.DeleteAsync(appConfig.Postgresql.ToString());

        }

        public async static Task IngestAsync(IngestConfiguration configuration)
        {
            Log.Information("[OwnedNumbers] Ingesting data for OwnedNumbers.");
            List<OwnedPhoneNumber> allNumbers = [];
            DateTime start = DateTime.Now;

            // Ingest all owned numbers from the providers.
            try
            {
                var firstComNumbers = await FirstPointComAsync(configuration.PComNetUsername, configuration.PComNetPassword);
                if (firstComNumbers != null)
                {
                    allNumbers.AddRange(firstComNumbers);
                };
            }
            catch (Exception ex)
            {
                Log.Fatal("[OwnedNumbers] Failed to retrieve owned numbers for FirstPointCom.");
                Log.Fatal(ex.Message);
                Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
            }

            try
            {
                OwnedPhoneNumber[] bulkVSNumbers = await TnRecord.GetOwnedAsync(configuration.BulkVSUsername, configuration.BulkVSPassword);
                if (bulkVSNumbers.Length != 0)
                {
                    allNumbers.AddRange(bulkVSNumbers);
                };
            }
            catch (Exception ex)
            {
                Log.Fatal("[OwnedNumbers] Failed to retrieve owned numbers for BulkVS.");
                Log.Fatal(ex.Message);
                Log.Fatal(ex?.StackTrace ?? "No stack trace found.");
            }

            // If we ingested any owned numbers update the database.
            IngestStatistics ownedNumberStats;
            if (allNumbers.Count > 0)
            {
                Log.Information($"[OwnedNumbers] Submitting {allNumbers.Count} numbers to the database.");
                ownedNumberStats = await SubmitOwnedNumbersAsync([.. allNumbers.DistinctBy(x => x.DialedNumber)], configuration.Postgresql, configuration.BulkVSUsername, configuration.BulkVSPassword);
            }
            else
            {
                Log.Fatal("[OwnedNumbers] No owned numbers ingested. Skipping submission to the database.");
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
                ServiceProviderChanged[] changedNumbers = await VerifyServiceProvidersAsync(configuration.BulkVSAPIKEY, configuration.Postgresql);

                if (changedNumbers.Length != 0)
                {
                    Log.Information($"[OwnedNumbers] Emailing out a notification that {changedNumbers.Length} numbers LRN updates.");
                    _ = await SendPortingNotificationEmailAsync(changedNumbers, configuration.SmtpUsername, configuration.SmtpPassword, configuration.EmailDan, configuration.EmailOrders, configuration.Postgresql);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal("[OwnedNumbers] Failed to look for LRN changes on owned numbers.");
                Log.Fatal(ex.Message);
            }

            // Offer unassigned phone numbers we own for purchase on the website.
            _ = await OfferUnassignedNumberForSaleAsync(configuration.Postgresql);

            // Match up owned numbers and their billingClients.
            _ = await MatchOwnedNumbersToBillingClientsAsync(configuration.Postgresql);

            // Link up E911 registrations to owned numbers.
            await VerifyEmergencyInformationAsync(configuration.Postgresql, configuration.BulkVSUsername, configuration.BulkVSPassword);

            // Match numbers to destinations and domains in FusionPBX.
            await MatchOwnedNumbersToFusionPBXAsync(configuration.Postgresql, configuration.FusionPBXUsername, configuration.FusionPBXPassword);

            // Update the statuses on old or orphaned port requests and ported numbers.
            await PortRequests.UpdatePortRequestsAndNumbersByExternalIdAsync(configuration);

            // Remove the lock from the database to prevent it from getting cluttered with blank entries.
            var lockEntry = await IngestStatistics.GetLockAsync("OwnedNumbers", configuration.Postgresql.ToString());
            if (lockEntry is not null)
            {
                _ = await lockEntry.DeleteAsync(configuration.Postgresql.ToString());
            }

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

            if (await combined.PostAsync(configuration.Postgresql.ToString()))
            {
                Log.Information("[OwnedNumbers] Completed the ingest process.");
            }
            else
            {
                Log.Fatal("[OwnedNumbers] Failed to completed the ingest process.");
            }
        }

        public readonly record struct SMSRouteChange(string DialedNumber, string OldRoute, string NewRoute, string Message);

        public static async Task<SMSRouteChange[]> VerifySMSRoutingAsync(ReadOnlyMemory<char> connectionString, ReadOnlyMemory<char> pComNetUsername, ReadOnlyMemory<char> pComNetPassword)
        {
            Log.Information($"[OwnedNumbers] Verifying SMS Routing for Owned Phone numbers.");

            List<SMSRouteChange> changes = [];

            var ownedNumbers = await OwnedPhoneNumber.GetAllAsync(connectionString.ToString());

            foreach (var number in ownedNumbers.Where(x => x.Status is "Active"))
            {
                bool updated = false;
                try
                {
                    var checkSMSRouting = await FirstPointCom.GetSMSRoutingByDialedNumberAsync($"1{number.DialedNumber}".AsMemory(), pComNetUsername, pComNetPassword);
                    if (!string.IsNullOrWhiteSpace(checkSMSRouting.route))
                    {
                        // Update the owned number with the route.
                        if (number.SMSRoute != checkSMSRouting.route)
                        {
                            changes.Add(new SMSRouteChange(number.DialedNumber, number.SMSRoute, checkSMSRouting.route, checkSMSRouting.QueryResult.text));
                            number.SMSRoute = checkSMSRouting.route;
                            updated = true;
                        }
                    }
                    else
                    {
                        Log.Error($"[OwnedNumbers] Could not verify SMS routing for {number.DialedNumber} with FirstPointCom. {checkSMSRouting.QueryResult.text}");
                        // Update the owned number with the route.
                        if (number.SMSRoute != checkSMSRouting.QueryResult.text)
                        {
                            changes.Add(new SMSRouteChange(number.DialedNumber, number.SMSRoute, checkSMSRouting.route, checkSMSRouting.QueryResult.text));
                            number.SMSRoute = checkSMSRouting.QueryResult.text;
                            updated = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[OwnedNumbers] Could not verify SMS routing for {number.DialedNumber} with FirstPointCom. {ex.Message}");
                }

                if (updated)
                {
                    var checkUpdate = await number.PutAsync(connectionString.ToString());
                    Log.Information($"[OwnedNumbers] Updated SMS routing for {number.DialedNumber} with FirstPointCom.");
                }
            }

            Log.Information($"[OwnedNumbers] Verified SMS Routing for {ownedNumbers.Count()} Owned Phone numbers.");

            return [.. changes];
        }

        public static async Task MatchOwnedNumbersToFusionPBXAsync(ReadOnlyMemory<char> connectionString, ReadOnlyMemory<char> fusionPBXUsername, ReadOnlyMemory<char> fusionPBXPassword)
        {
            Log.Information($"[OwnedNumbers] Matching FusionPBX data for Owned Phone numbers.");

            var ownedNumbers = await OwnedPhoneNumber.GetAllAsync(connectionString.ToString()).ConfigureAwait(false);

            try
            {
                DestinationDetails destination = await DestinationDetails.GetByDialedNumberAsync("2068588757".AsMemory(), fusionPBXUsername, fusionPBXPassword);

                // If we aren't getting good data back from FusionPBX then skip this updating process.
                if (!string.IsNullOrWhiteSpace(destination.domain_uuid))
                {
                    foreach (var ownedNumber in ownedNumbers.Where(x => x.Active))
                    {
                        bool updated = false;

                        try
                        {
                            destination = await DestinationDetails.GetByDialedNumberAsync(ownedNumber.DialedNumber.AsMemory(), fusionPBXUsername, fusionPBXPassword);

                            if (!string.IsNullOrWhiteSpace(destination.domain_uuid))
                            {
                                var checkDestinationuuid = Guid.TryParse(destination.destination_uuid, out var parsedDestionationId);

                                if (ownedNumber.FPBXDestinationId != parsedDestionationId)
                                {
                                    ownedNumber.FPBXDestinationId = parsedDestionationId;
                                    updated = true;
                                }

                                var domain = await DomainDetails.GetByDomainIdAsync(destination.domain_uuid.AsMemory(), fusionPBXUsername, fusionPBXPassword);

                                if (!string.IsNullOrWhiteSpace(domain.domain_name))
                                {
                                    if (ownedNumber.FPBXDomainName != domain.domain_name)
                                    {
                                        ownedNumber.FPBXDomainName = domain.domain_name;
                                        updated = true;
                                    }

                                    if (ownedNumber.FPBXDomainDescription != domain.domain_description)
                                    {
                                        ownedNumber.FPBXDomainDescription = domain.domain_description;
                                        updated = true;
                                    }

                                    var checkDomainuuid = Guid.TryParse(domain.domain_uuid, out var parsedDomainId);

                                    if (checkDomainuuid && ownedNumber.FPBXDomainId != parsedDomainId)
                                    {
                                        ownedNumber.FPBXDomainId = parsedDomainId;
                                        updated = true;
                                    }
                                }
                            }
                        }
                        catch (FlurlHttpException ex)
                        {
                            var message = await ex.GetResponseStringAsync();
                            Log.Warning("[OwnedNumbers] Failed to find destination and domain information for owned number {@OwnedNumber} : {FailureMessage} : {StatusCode}", ownedNumber, message, ex.StatusCode);

                            // If we can't find the number remove the existing data.
                            if (ex.StatusCode is 404)
                            {
                                updated = true;
                                ownedNumber.FPBXDestinationId = null;
                                ownedNumber.FPBXDomainName = string.Empty;
                                ownedNumber.FPBXDomainDescription = string.Empty;
                                ownedNumber.FPBXDomainId = null;
                            }
                        }

                        if (updated)
                        {
                            ownedNumber.DateUpdated = DateTime.Now;
                            _ = await ownedNumber.PutAsync(connectionString.ToString());
                            Log.Information("[OwnedNumbers] Updated FusionPBX data for Owned Phone number {@OwnedNumber}", ownedNumber);
                        }
                    }
                }
            }
            catch (FlurlHttpException ex)
            {
                var message = await ex.GetResponseStringAsync();
                Log.Warning("[OwnedNumbers] Failed to find destination and domain information for known good owned number {FailureMessage} : {StatusCode}", message, ex.StatusCode);
            }

            Log.Information($"[OwnedNumbers] Updated FusionPBX data for Owned Phone numbers.");
        }

        public static async Task<IEnumerable<OwnedPhoneNumber>> FirstPointComAsync(ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            List<OwnedPhoneNumber> numbers = [];

            foreach (int npa in AreaCodes.All)
            {
                try
                {
                    var results = await FirstPointCom.GetOwnedPhoneNumbersAsync(npa, username, password);

                    Log.Information($"[OwnedNumbers] [FirstPointCom] Retrieved {results.DIDOrder.Length} owned numbers.");

                    foreach (var item in results.DIDOrder)
                    {
                        var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.DID, out var phoneNumber);

                        if (checkParse)
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

            return numbers.Count != 0 ? [.. numbers] : [];
        }

        public static async Task<IngestStatistics> SubmitOwnedNumbersAsync(OwnedPhoneNumber[] newlyIngested, ReadOnlyMemory<char> connectionString, ReadOnlyMemory<char> bulkVSUsername, ReadOnlyMemory<char> bulkVSPassword)
        {
            DateTime start = DateTime.Now;
            int ingestedNew = 0;
            int updatedExisting = 0;

            try
            {
                var existingOwnedNumbers = await OwnedPhoneNumber.GetAllAsync(connectionString.ToString());
                var existingAsDict = existingOwnedNumbers.DistinctBy(x => x.DialedNumber).ToDictionary(x => x.DialedNumber, x => x);
                var newAsDict = newlyIngested.ToDictionary(x => x.DialedNumber, x => x);
                var portedPhoneNumbers = await PortedPhoneNumber.GetAllAsync(connectionString.ToString());

                foreach (OwnedPhoneNumber item in newlyIngested)
                {
                    var checkExisting = existingAsDict.TryGetValue(item.DialedNumber, out var number);

                    if (checkExisting is false || number is null)
                    {
                        var matchingPort = portedPhoneNumbers.Where(x => x.PortedDialedNumber == item.DialedNumber).FirstOrDefault();
                        if (matchingPort is not null && !string.IsNullOrWhiteSpace(matchingPort.RequestStatus) && matchingPort.RequestStatus is not "COMPLETE")
                        {
                            TnRecord externalStatus = await TnRecord.GetByDialedNumberAsync(item.DialedNumber.AsMemory(), bulkVSUsername, bulkVSPassword);
                            if (externalStatus.Status is not "Active")
                            {
                                // If it is a ported number and the port has not complete mark it as ported in.
                                item.Status = "Porting In";
                            }
                            else
                            {
                                item.Status = "Active";
                            }
                        }
                        else
                        {
                            // If it's not a ported number or the porting is complete just mark it as active.
                            item.Status = "Active";
                        }
                        item.Active = true;
                        // Add new owned numbers.
                        var checkCreate = await item.PostAsync(connectionString.ToString());
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
                        // Update existing owned numbers.
                        var matchingPort = portedPhoneNumbers.Where(x => x.PortedDialedNumber == item.DialedNumber).FirstOrDefault();
                        if (matchingPort is not null && !string.IsNullOrWhiteSpace(matchingPort.RequestStatus) && matchingPort.RequestStatus is not "COMPLETE")
                        {
                            var externalStatus = await TnRecord.GetByDialedNumberAsync(item.DialedNumber.AsMemory(), bulkVSUsername, bulkVSPassword);
                            if (externalStatus.Status is not "Active")
                            {
                                // If it is a ported number and the port has not complete mark it as ported in.
                                number.Status = "Porting In";
                            }
                            else
                            {
                                number.Status = "Active";
                            }
                        }
                        else
                        {
                            // If it's not a ported number or the porting is complete just mark it as active.
                            number.Status = "Active";
                        }
                        number.Notes = string.IsNullOrWhiteSpace(number.Notes) ? item.Notes : number.Notes;
                        number.IngestedFrom = item.IngestedFrom;
                        number.DateUpdated = item.DateIngested;
                        number.TrunkGroup = item.TrunkGroup;
                        number.Active = true;

                        var checkCreate = await number.PutAsync(connectionString.ToString());
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

                OwnedPhoneNumber[] unmatchedExistingNumbers = existingOwnedNumbers.Where(x => !newAsDict.ContainsKey(x.DialedNumber)).ToArray();

                foreach (var item in unmatchedExistingNumbers)
                {
                    // Mark unmatched Numbers as inactive.
                    item.Active = false;
                    if (item.Status is not "Cancelled" || item.Status is not "Porting Out")
                    {
                        item.Status = "Cancelled";
                    }
                    var checkCreate = await item.PutAsync(connectionString.ToString());
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

                DateTime end = DateTime.Now;

                IngestStatistics stats = new()
                {
                    StartDate = start,
                    EndDate = end,
                    IngestedFrom = "OwnedNumbers",
                    NumbersRetrived = newlyIngested.Length,
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
            catch (Exception ex)
            {
                Log.Fatal($"{ex.Message}");
                Log.Fatal($"{ex.StackTrace}");
                return new()
                {
                    StartDate = start,
                    EndDate = DateTime.Now,
                    IngestedFrom = "OwnedNumbers",
                    NumbersRetrived = newlyIngested.Length,
                    Priority = false,
                    Lock = false,
                    IngestedNew = ingestedNew,
                    UpdatedExisting = updatedExisting,
                    Removed = 0,
                    Unchanged = 0,
                    FailedToIngest = 0
                };
            }
        }

        public static async Task<IngestStatistics> OfferUnassignedNumberForSaleAsync(ReadOnlyMemory<char> connectionString)
        {
            DateTime start = DateTime.Now;
            int ingestedNew = 0;
            int updatedExisting = 0;

            var numbers = await OwnedPhoneNumber.GetAllAsync(connectionString.ToString());

            List<DataAccess.Models.PhoneNumber> newUnassigned = [];

            foreach (var item in numbers)
            {
                if (!string.IsNullOrWhiteSpace(item?.Notes) && item?.Notes.Trim() == "Unassigned")
                {
                    var number = await DataAccess.Models.PhoneNumber.GetAsync(item.DialedNumber, connectionString.ToString());

                    if (number is null || item.DialedNumber != number.DialedNumber)
                    {
                        var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item?.DialedNumber ?? string.Empty, out var phoneNumber);

                        if (checkParse)
                        {
                            number = new DataAccess.Models.PhoneNumber
                            {
                                NPA = phoneNumber.NPA,
                                NXX = phoneNumber.NXX,
                                XXXX = phoneNumber.XXXX,
                                DialedNumber = phoneNumber.DialedNumber ?? string.Empty,
                                DateIngested = item?.DateIngested ?? DateTime.Now,
                                IngestedFrom = "OwnedNumber",
                                Purchased = false
                            };

                            newUnassigned.Add(number);
                            ingestedNew++;

                            Log.Information($"[Ingest] [OwnedNumber] Put unassigned number {item?.DialedNumber} up for sale.");
                        }
                        else
                        {
                            Log.Fatal($"[Ingest] [OwnedNumber] Failed to put unassigned number {item?.DialedNumber} up for sale. Number could not be parsed.");
                        }
                    }
                    else
                    {
                        number.DateIngested = item.DateIngested;
                        number.IngestedFrom = "OwnedNumber";
                        number.Purchased = false;

                        _ = await number.PutAsync(connectionString.ToString());
                        updatedExisting++;

                        Log.Information($"[Ingest] [OwnedNumber] Continued offering unassigned number {item.DialedNumber} up for sale.");
                    }
                }
            }

            DataAccess.Models.PhoneNumber[] typedNumbers = Services.AssignNumberTypes([.. newUnassigned]);
            DataAccess.Models.PhoneNumber[] locations = await Services.AssignRatecenterAndRegionAsync(typedNumbers);
            _ = await Services.SubmitPhoneNumbersAsync(locations, connectionString);

            DateTime end = DateTime.Now;

            IngestStatistics stats = new()
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

        public static async Task<IngestStatistics> MatchOwnedNumbersToBillingClientsAsync(ReadOnlyMemory<char> connectionString)
        {
            DateTime start = DateTime.Now;
            int updatedExisting = 0;

            var numbers = await OwnedPhoneNumber.GetAllAsync(connectionString.ToString());
            var purchased = await PurchasedPhoneNumber.GetAllAsync(connectionString.ToString());
            var ported = await PortedPhoneNumber.GetAllAsync(connectionString.ToString());

            foreach (var number in numbers)
            {
                var match = purchased.Where(x => x.DialedNumber == number.DialedNumber).FirstOrDefault();

                if (match is null)
                {
                    var match2 = ported.Where(x => x.PortedDialedNumber == number.DialedNumber).FirstOrDefault();

                    if (match2 is null)
                    {
                        // Do nothing if we can't find a match in our system for this number.
                        Log.Information($"[OwnedNumbers] [ClientMatch] Couldn't associate Owned Number {number.DialedNumber} with a billing client.");
                        continue;
                    }

                    var order = await Order.GetByIdAsync(match2.OrderId ?? Guid.NewGuid(), connectionString.ToString());

                    //var badLink = $"https://billing.acceleratenetworks.com/clients/{number.BillingClientId}/edit";
                    //var checkLink = badLink.GetStringAsync();

                    if (order is not null && !string.IsNullOrWhiteSpace(order?.BillingClientId))
                    {
                        number.BillingClientId = order.BillingClientId;
                        number.OwnedBy = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName;

                        var checkUpdate = await number.PutAsync(connectionString.ToString());
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
                    var order = await Order.GetByIdAsync(match.OrderId, connectionString.ToString());

                    if (order is not null && !string.IsNullOrWhiteSpace(order?.BillingClientId))
                    {
                        number.BillingClientId = order.BillingClientId;
                        number.OwnedBy = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName;

                        var checkUpdate = await number.PutAsync(connectionString.ToString());
                        updatedExisting++;

                        Log.Information($"[OwnedNumbers] [ClientMatch] Associated Owned Number {match?.DialedNumber} with billing client id {order?.BillingClientId}");
                    }
                    else
                    {
                        Log.Information($"[OwnedNumbers] [ClientMatch] Couldn't associate Owned Number {match?.DialedNumber} with a billing client.");
                    }
                }
            }

            DateTime end = DateTime.Now;

            IngestStatistics stats = new()
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

        public readonly record struct ServiceProviderChanged
        (
             string DialedNumber,
             string OldSPID,
             string CurrentSPID,
             string OldSPIDName,
             string CurrentSPIDName,
             string RawQuery
        );

        public static async Task<ServiceProviderChanged[]> VerifyServiceProvidersAsync(ReadOnlyMemory<char> bulkApiKey, ReadOnlyMemory<char> connectionString)
        {
            var owned = await OwnedPhoneNumber.GetAllAsync(connectionString.ToString());
            List<ServiceProviderChanged> serviceProviderChanged = [];

            // Only query data for numbers with a status of Active.
            foreach (var number in owned.Where(x => x.Status is "Active"))
            {
                var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(number.DialedNumber, out var phoneNumber);

                if (checkParse && !string.IsNullOrWhiteSpace(phoneNumber.DialedNumber))
                {
                    if (phoneNumber.DialedNumber.IsTollfree())
                    {
                        // Skip the LRNlookup if the current number is TollFree.
                        Log.Information($"[OwnedNumbers] Skipping Toll Free number {number.DialedNumber}.");
                    }
                    else
                    {
                        var result = await LrnBulkCnam.GetAsync(number.DialedNumber.AsMemory(), bulkApiKey);

                        var provider = "BulkVS";
                        var newSpid = result.spid ?? string.Empty;
                        var newSpidName = result.lec ?? string.Empty;

                        var updatedSPID = newSpid != number.SPID && !string.IsNullOrWhiteSpace(newSpid);
                        var updatedSPIDName = newSpidName != number.SPIDName && !string.IsNullOrWhiteSpace(newSpidName);

                        if (updatedSPID && updatedSPIDName)
                        {
                            serviceProviderChanged.Add(new ServiceProviderChanged
                            {
                                CurrentSPID = newSpid,
                                OldSPID = number.SPID,
                                CurrentSPIDName = newSpidName,
                                OldSPIDName = number.SPIDName,
                                DialedNumber = number.DialedNumber,
                                RawQuery = JsonSerializer.Serialize(result)
                            });

                            // Update the SPID to the current value.
                            number.SPID = newSpid;
                            number.SPIDName = newSpidName;
                            var checkUpdate = await number.PutAsync(connectionString.ToString());
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
                            // Do nothing because either the SPID is the same or it's invalid.
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

            return [.. serviceProviderChanged];
        }

        public static async Task VerifyEmergencyInformationAsync(ReadOnlyMemory<char> connectionString, ReadOnlyMemory<char> bulkVSUsername, ReadOnlyMemory<char> bulkVSPassword)
        {
            var emergencyInformation = await EmergencyInformation.GetAllAsync(connectionString.ToString());

            Log.Information($"[OwnedNumbers] Verifying Emergency Information for {emergencyInformation?.Count()} Owned Phone numbers.");

            var ownedNumbers = await OwnedPhoneNumber.GetAllAsync(connectionString.ToString());

            var e911Registrations = await E911Record.GetAllAsync(bulkVSUsername, bulkVSPassword);

            foreach (var record in e911Registrations)
            {
                var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(record.TN, out var number);

                var ownedNumber = ownedNumbers.FirstOrDefault(x => x.DialedNumber == number.DialedNumber);
                if (checkParse && ownedNumber is not null && ownedNumber.EmergencyInformationId is not null && ownedNumber.EmergencyInformationId.HasValue)
                {
                    var existing = emergencyInformation?.FirstOrDefault(x => x.EmergencyInformationId == ownedNumber.EmergencyInformationId.GetValueOrDefault());

                    if (existing is not null && existing.DialedNumber == number.DialedNumber)
                    {
                        // Update the existing record with the current data.
                        existing.Sms = record.Sms.Length != 0 ? string.Join(',', record.Sms) : string.Empty;
                        existing.RawResponse = JsonSerializer.Serialize(record);
                        existing.BulkVSLastModificationDate = existing.BulkVSLastModificationDate != record.LastModification ? record.LastModification : existing.BulkVSLastModificationDate;
                        existing.ModifiedDate = DateTime.Now;
                        existing.CallerName = existing.CallerName != record.CallerName ? record.CallerName : existing.CallerName;
                        existing.AddressLine1 = existing.AddressLine1 != record.AddressLine1 ? record.AddressLine1 : existing.AddressLine1;
                        existing.AddressLine2 = existing.AddressLine2 != record.AddressLine2 ? record.AddressLine2 : existing.AddressLine2;
                        existing.City = existing.City != record.City ? record.City : existing.City;
                        existing.State = existing.State != record.State ? record.State : existing.State;
                        existing.Zip = existing.Zip != record.Zip ? record.Zip : existing.Zip;

                        var checkUpdate = await existing.PutAsync(connectionString.ToString());

                        if (!checkUpdate)
                        {
                            Log.Error($"Failed to update E911 record for {existing.DialedNumber} from {JsonSerializer.Serialize(record)}");
                        }
                    }
                    else
                    {
                        // Create a new record to replace the existing one.
                        var registration = new EmergencyInformation
                        {
                            DialedNumber = number.DialedNumber,
                            Sms = record.Sms.Length != 0 ? string.Join(',', record.Sms) : string.Empty,
                            State = record.State,
                            City = record.City,
                            Zip = record.Zip,
                            CallerName = record.CallerName,
                            AddressLine1 = record.AddressLine1,
                            AddressLine2 = record.AddressLine2,
                            BulkVSLastModificationDate = record.LastModification,
                            DateIngested = DateTime.Now,
                            IngestedFrom = "BulkVS",
                            ModifiedDate = DateTime.Now,
                            RawResponse = JsonSerializer.Serialize(record),
                        };

                        ownedNumber.EmergencyInformationId = registration.EmergencyInformationId;

                        var checkCreate = await registration.PostAsync(connectionString.ToString());
                        var checkUpdate = await ownedNumber.PutAsync(connectionString.ToString());

                        if (!checkCreate && !checkUpdate)
                        {
                            Log.Error($"Failed to create E911 record for {number.DialedNumber} from {JsonSerializer.Serialize(record)}");
                        }
                    }
                }
                else if (checkParse && ownedNumber is not null && ownedNumber.DialedNumber == number.DialedNumber)
                {
                    // Create a new E911 record if none is linked to this owned number.
                    var registration = new EmergencyInformation
                    {
                        DialedNumber = number.DialedNumber,
                        Sms = record.Sms.Length != 0 ? string.Join(',', record.Sms) : string.Empty,
                        State = record.State,
                        City = record.City,
                        Zip = record.Zip,
                        CallerName = record.CallerName,
                        AddressLine1 = record.AddressLine1,
                        AddressLine2 = record.AddressLine2,
                        BulkVSLastModificationDate = record.LastModification,
                        DateIngested = DateTime.Now,
                        IngestedFrom = "BulkVS",
                        ModifiedDate = DateTime.Now,
                        RawResponse = JsonSerializer.Serialize(record),
                    };

                    ownedNumber.EmergencyInformationId = registration.EmergencyInformationId;

                    var checkCreate = await registration.PostAsync(connectionString.ToString());
                    var checkUpdate = await ownedNumber.PutAsync(connectionString.ToString());

                    if (!checkCreate && !checkUpdate)
                    {
                        Log.Error($"Failed to create E911 record for {number.DialedNumber} from {JsonSerializer.Serialize(record)}");
                    }
                }

                // Do nothing if we can't find a matching owned number for this e911 registration.
            }
        }

        public static async Task<bool> SendPortingNotificationEmailAsync(ServiceProviderChanged[] changes, ReadOnlyMemory<char> smtpUsername, ReadOnlyMemory<char> smtpPassword, ReadOnlyMemory<char> emailPrimary, ReadOnlyMemory<char> emailCC, ReadOnlyMemory<char> connectionString)
        {
            if (changes.Length is 0)
            {
                // Successfully did nothing.
                return true;
            }

            JsonSerializerOptions options = new()
            {
                WriteIndented = true
            };

            var output = new StringBuilder();

            output.Append(JsonSerializer.Serialize(changes, options));

            var notificationEmail = new DataAccess.Models.Email
            {
                PrimaryEmailAddress = emailPrimary.ToString(),
                CarbonCopy = emailCC.ToString(),
                DateSent = DateTime.Now,
                Subject = $"[Ingest] {changes.Length} phone numbers changed Service Providers.",
                MessageBody = output.ToString(),
                OrderId = new Guid(),
                Completed = true
            };

            var checkSend = await notificationEmail.SendEmailAsync(smtpUsername.ToString(), smtpPassword.ToString());
            var checkSave = await notificationEmail.PostAsync(connectionString.ToString());

            return checkSave && checkSend;
        }
    }
}