using FirstCom;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.Models;
using NumberSearch.DataAccess.TeleMesssage;

using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public class Owned
    {
        public static async Task<IEnumerable<OwnedPhoneNumber>> FirstPointComAsync(string username, string password)
        {
            var numbers = new List<OwnedPhoneNumber>();

            var areaCodes = AreaCode.All;

            foreach (var npa in areaCodes)
            {
                var results = await FirstPointComOwnedPhoneNumber.GetAsync(npa.ToString(), username, password).ConfigureAwait(false);

                foreach (var item in results.DIDOrder)
                {
                    bool checkNpa = int.TryParse(item.NPA, out int outNpa);
                    bool checkNxx = int.TryParse(item.NXX, out int outNxx);
                    bool checkXxxx = int.TryParse(item.DID.Substring(7), out int outXxxx);

                    if (checkNpa && outNpa < 1000 && checkNxx && outNxx < 1000 && checkXxxx && outXxxx < 10000 && item.DID.Length == 11)
                    {
                        numbers.Add(new OwnedPhoneNumber
                        {

                            DialedNumber = item.DID.Substring(1),
                            IngestedFrom = "FirstPointCom",
                            Active = true,
                            DateIngested = DateTime.Now
                        });
                    }
                    else
                    {
                        Log.Error($"This failed the 11 char check {item.DID.Length}");
                    }
                }
            }

            Log.Information($"[OwnedNumbers] [FirstPointCom] Ingested {numbers.Count} owned numbers.");

            return numbers.ToArray();
        }

        public static async Task<IEnumerable<OwnedPhoneNumber>> TeleMessageAsync(Guid token)
        {
            var results = await UserDidsList.GetAllAsync(token).ConfigureAwait(false);

            var list = new List<OwnedPhoneNumber>();

            foreach (var item in results?.data)
            {
                bool checkNpa = int.TryParse(item.npa, out int npa);
                bool checkNxx = int.TryParse(item.nxx, out int nxx);
                bool checkXxxx = int.TryParse(item.xxxx, out int xxxx);

                if (checkNpa && checkNxx && checkXxxx)
                {
                    var number = new OwnedPhoneNumber
                    {
                        DialedNumber = item.number,
                        IngestedFrom = "TeleMessage",
                        Active = true,
                        DateIngested = DateTime.Now,
                        Notes = item.note
                    };

                    if (number.IngestedFrom == "TeleMessage")
                    {
                        // Enabled CNAM on Teli numbers every time they are ingested.
                        try
                        {
                            var cnamEnable = await UserDidsCnamEnable.GetAsync(number.DialedNumber, token).ConfigureAwait(false);
                            var result = await UserDidsGet.GetAsync(number.DialedNumber, token).ConfigureAwait(false);
                            var lidb = await UserDidsLibdGet.GetAsync(result?.data?.id, token).ConfigureAwait(false);
                            number.LIDBCNAM = lidb?.data ?? string.Empty;
                            Log.Information($"[OwnedNumber] [TeleMessage] LIDB CNAM {lidb?.data} for {number.DialedNumber} or did_id {result?.data?.id} is enabled? {cnamEnable.CnamEnabled()}.");
                        }
                        catch
                        {
                            Log.Fatal($"[OwnedNumber] [TeleMessage] Failed to enable CNAM for {number.DialedNumber}.");
                        }
                    }

                    list.Add(number);

                }
            }

            Log.Information($"[OwnedNumbers] [TeleMessage] Ingested {list.Count} owned numbers.");

            return list;
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
                    ingestedNew++;
                }
                else
                {
                    item.Notes = string.IsNullOrWhiteSpace(number.Notes) ? item.Notes : number.Notes;

                    var checkCreate = await item.PutAsync(connectionString).ConfigureAwait(false);
                    updatedExisting++;
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

        public static async Task<IngestStatistics> OfferUnassignedNumberForSaleAsync(string bulkVSAPIKey, string connectionString)
        {
            var start = DateTime.Now;
            var ingestedNew = 0;
            var updatedExisting = 0;

            var numbers = await OwnedPhoneNumber.GetAllAsync(connectionString).ConfigureAwait(false);

            var newUnassigned = new List<PhoneNumber>();

            foreach (var item in numbers)
            {
                if (item?.Notes != null && item?.Notes.Trim() == "Unassigned")
                {
                    var number = await PhoneNumber.GetAsync(item.DialedNumber, connectionString).ConfigureAwait(false);

                    if (number is null || string.IsNullOrWhiteSpace(number?.DialedNumber))
                    {
                        // If the number has at least 10 chars then it could be a valid phone number.
                        // If the number starts with a 1 then it's a US number, we want to ignore internation numbers.
                        if (item.DialedNumber.Length == 10 || item.DialedNumber.Length == 11)
                        {
                            item.DialedNumber = item.DialedNumber.Substring(item.DialedNumber.Length - 10);
                        }
                        else
                        {
                            Log.Warning($"[Ingest] [OwnedNumber] Failed to parse {item.DialedNumber}. Passed neither the 10 or 11 char checks.");
                            continue;
                        }

                        bool checkNpa = int.TryParse(item.DialedNumber.Substring(0, 3), out int npa);
                        bool checkNxx = int.TryParse(item.DialedNumber.Substring(3, 3), out int nxx);
                        bool checkXxxx = int.TryParse(item.DialedNumber.Substring(6, 4), out int xxxx);

                        if (checkNpa && checkNxx && checkXxxx)
                        {
                            var lrnInfo = await LrnBulkCnam.GetAsync(item.DialedNumber, bulkVSAPIKey).ConfigureAwait(false);

                            number = new PhoneNumber
                            {
                                NPA = npa,
                                NXX = nxx,
                                XXXX = xxxx,
                                DialedNumber = item.DialedNumber,
                                City = string.IsNullOrWhiteSpace(lrnInfo.city) ? "Unknown City" : lrnInfo.city,
                                State = string.IsNullOrWhiteSpace(lrnInfo.province) ? "Unknown State" : lrnInfo.province,
                                DateIngested = item.DateIngested,
                                IngestedFrom = "OwnedNumber"
                            };
                        }

                        newUnassigned.Add(number);
                        ingestedNew++;

                        Log.Information($"[Ingest] [OwnedNumber] Put unassigned number {item.DialedNumber} up for sale.");
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

            var unassignedNumberStats = await Services.SubmitPhoneNumbersAsync(typedNumbers, connectionString).ConfigureAwait(false);

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
                        number.BillingClientId = order?.BillingClientId;
                        number.OwnedBy = string.IsNullOrWhiteSpace(order?.BusinessName) ? $"{order?.FirstName} {order?.LastName}" : order?.BusinessName;

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
                        number.BillingClientId = order?.BillingClientId;
                        number.OwnedBy = string.IsNullOrWhiteSpace(order?.BusinessName) ? $"{order?.FirstName} {order?.LastName}" : order?.BusinessName;

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
            public string DialedNumber { get; set; }
            public string OldSPID { get; set; }
            public string CurrentSPID { get; set; }
            public string OldSPIDName { get; set; }
            public string CurrentSPIDName { get; set; }
        }

        public static async Task<IEnumerable<ServiceProviderChanged>> VerifyServiceProvidersAsync(Guid teleToken, string bulkApiKey, string connectionString)
        {
            var owned = await OwnedPhoneNumber.GetAllAsync(connectionString).ConfigureAwait(false);
            var tollFree = AreaCode.TollFree;

            var serviceProviderChanged = new List<ServiceProviderChanged>();

            foreach (var number in owned)
            {
                var npa = number.DialedNumber.Substring(0, 3);
                var isTollFree = tollFree.Where(x => x.ToString() == npa).Any();

                if (isTollFree)
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

                    var checkSPID = newSpid != number.SPID;
                    var checkSPIDName = newSpidName != number.SPIDName;

                    if (checkSPID || checkSPIDName)
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
                    }

                    Log.Information($"[OwnedNumbers] Found {newSpidName}, {newSpid} for {number.DialedNumber} from [{provider}].");
                }
            }

            Log.Information($"[OwnedNumbers] Found {serviceProviderChanged.Count} numbers whose Service Provider has changed since the last ingest.");

            return serviceProviderChanged;
        }

        public static async Task<IEnumerable<OwnedPhoneNumber>> VerifyEmergencyInformationAsync(IEnumerable<OwnedPhoneNumber> owned, Guid teleToken, string connectionString)
        {
            Log.Information($"[OwnedNumbers] Verifying Emergency Information for {owned?.Count()} Owned Phone numbers.");

            var emergencyInformation = await EmergencyInformation.GetAllAsync(connectionString).ConfigureAwait(false);

            foreach (var number in owned)
            {
                var info = await EmergencyInfo.GetAsync(number.DialedNumber, teleToken).ConfigureAwait(false);

                if (info?.code == 200)
                {
                    var checkCreate = DateTime.TryParse(info?.data?.create_dt, out var createdDate);
                    var checkMod = DateTime.TryParse(info?.data?.modify_dt, out var modDate);

                    var toDb = new EmergencyInformation
                    {
                        Address = info?.data?.address,
                        AlertGroup = info?.data?.alert_group,
                        City = info?.data?.city,
                        CreatedDate = checkCreate ? createdDate : DateTime.Now,
                        DateIngested = DateTime.Now,
                        DialedNumber = number.DialedNumber,
                        FullName = info?.data?.full_name,
                        IngestedFrom = "TeleMessage",
                        ModifyDate = checkMod ? modDate : DateTime.Now,
                        Note = info?.data?.did_note.Trim(),
                        State = info?.data?.state,
                        TeliId = info?.data?.did_id,
                        UnitNumber = info?.data?.unit_number,
                        UnitType = info?.data?.unit_type,
                        Zip = info?.data?.zip
                    };

                    var existing = emergencyInformation.Where(x => x.DialedNumber == toDb.DialedNumber).FirstOrDefault();

                    if (existing is null)
                    {
                        var checkSubmit = await toDb.PostAsync(connectionString).ConfigureAwait(false);

                        Log.Information($"[OwnedNumbers] Added Emergency Information for {number.DialedNumber}.");
                    }
                    else
                    {
                        var checkSubmit = await toDb.PutAsync(connectionString).ConfigureAwait(false);

                        Log.Information($"[OwnedNumbers] Updated Emergency Information for {number.DialedNumber}.");
                    }
                }
            }

            var fromDb = await EmergencyInformation.GetAllAsync(connectionString).ConfigureAwait(false);

            foreach (var item in owned)
            {
                var emergencyInfo = fromDb.Where(x => x.DialedNumber == item.DialedNumber).FirstOrDefault();

                if (!(emergencyInfo is null))
                {
                    item.EmergencyInformationId = emergencyInfo?.EmergencyInformationId;
                }
            }

            return owned;
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