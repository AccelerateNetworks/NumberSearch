using BulkVS;

using FirstCom;

using NumberSearch.DataAccess;
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
                    list.Add(new OwnedPhoneNumber
                    {
                        DialedNumber = item.number,
                        IngestedFrom = "TeleMessage",
                        Active = true,
                        DateIngested = DateTime.Now,
                        Notes = item.note
                    });
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
                    number.DateIngested = item.DateIngested;
                    number.IngestedFrom = item.IngestedFrom;
                    number.Notes = string.IsNullOrWhiteSpace(number.Notes) ? item.Notes : number.Notes;

                    var checkCreate = await number.PutAsync(connectionString).ConfigureAwait(false);
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
                    var result = await LrnLookup.GetAsync(number.DialedNumber, teleToken).ConfigureAwait(false);

                    var provider = "TeleMessage";
                    var newSpid = result.data.spid;
                    var newSpidName = result.data.spid_name;

                    if (string.IsNullOrWhiteSpace(newSpid) || string.IsNullOrWhiteSpace(newSpidName))
                    {
                        var bulkResult = await LrnBulkCnam.GetAsync(number.DialedNumber, bulkApiKey).ConfigureAwait(false);

                        provider = "BulkVS";
                        newSpid = bulkResult?.spid ?? string.Empty;
                        newSpidName = bulkResult?.lec ?? string.Empty;
                    }

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

        public static async Task<bool> SendPortingNotificationEmailAsync(IEnumerable<ServiceProviderChanged> changes, string smtpUsername, string smtpPassword, string connectionString)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var output = new StringBuilder();

            foreach (var change in changes)
            {
                output.Append(JsonSerializer.Serialize(change, options));
            }

            var notificationEmail = new Email
            {
                PrimaryEmailAddress = "orders@acceleratenetworks.com",
                DateSent = DateTime.Now,
                Subject = $"[Ingest] {changes.Count()} phone numbers changed Service Providers.",
                MessageBody = output.ToString()
            };

            return await notificationEmail.SendEmailAsync(smtpUsername, smtpPassword).ConfigureAwait(false);
        }
    }
}