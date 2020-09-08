using FirstCom;

using NumberSearch.DataAccess;

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
        public static async Task<IEnumerable<PhoneNumber>> FirstPointComAsync(string username, string password, string connectionString)
        {
            var results = await FirstPointComOwnedPhoneNumber.GetAllAsync(string.Empty, username, password).ConfigureAwait(false);

            var numbers = new List<PhoneNumber>();

            foreach (var item in results.DIDOrder)
            {
                bool checkNpa = int.TryParse(item.NPA, out int outNpa);
                bool checkNxx = int.TryParse(item.NXX, out int outNxx);
                bool checkXxxx = int.TryParse(item.DID.Substring(7), out int outXxxx);

                if (checkNpa && outNpa < 1000 && checkNxx && outNxx < 1000 && checkXxxx && outXxxx < 10000 && item.DID.Length == 11)
                {
                    numbers.Add(new PhoneNumber
                    {
                        NPA = outNpa,
                        NXX = outNxx,
                        XXXX = outXxxx,
                        DialedNumber = item.DID.Substring(1),
                        City = "Unknown City",
                        State = "Unknown State",
                        IngestedFrom = "FirstPointCom"
                    });
                }
                else
                {
                    Log.Error($"This failed the 11 char check {item.DID.Length}");
                }
            }

            return numbers.ToArray();
        }

        public static async Task<IngestStatistics> SubmitOwnedNumbersAsync(IEnumerable<PhoneNumber> numbers, string connectionString)
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
                    number = new OwnedPhoneNumber
                    {
                        DialedNumber = item.DialedNumber,
                        Active = true,
                        DateIngested = DateTime.Now,
                        IngestedFrom = "FirstPointCom",
                        Notes = string.Empty,
                        OwnedBy = string.Empty,
                        BillingClientId = string.Empty
                    };
                }
                else
                {
                    number.Active = true;
                    number.DateIngested = DateTime.Now;
                    number.IngestedFrom = "FirstPointCom";
                }

                if (number.OwnedPhoneNumberId == Guid.Empty)
                {
                    var checkCreate = number.PostAsync(connectionString).ConfigureAwait(false);
                    ingestedNew++;
                }
                else
                {
                    var checkCreate = number.PutAsync(connectionString).ConfigureAwait(false);
                    updatedExisting++;
                }
            }

            var end = DateTime.Now;

            var stats = new IngestStatistics
            {
                StartDate = start,
                EndDate = end,
                IngestedFrom = "FirstPointCom",
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

        public class ServiceProviderChanged
        {
            public string DialedNumber { get; set; }
            public string OldSPID { get; set; }
            public string CurrentSPID { get; set; }
        }

        public static async Task<IEnumerable<ServiceProviderChanged>> VerifyServiceProvidersAsync(Guid teleToken, string connectionString)
        {
            var owned = await OwnedPhoneNumber.GetAllAsync(connectionString).ConfigureAwait(false);

            var serviceProviderChanged = new List<ServiceProviderChanged>();

            foreach (var number in owned)
            {
                var result = await LrnLookup.GetAsync(number.DialedNumber, teleToken).ConfigureAwait(false);

                var checkChanged = result.data.spid != number.SPID;

                if (checkChanged)
                {
                    serviceProviderChanged.Add(new ServiceProviderChanged
                    {
                        CurrentSPID = result.data.spid,
                        OldSPID = number.SPID,
                        DialedNumber = number.DialedNumber
                    });
                }
            }

            return serviceProviderChanged;
        }

        public static async Task<bool> SendPortingNotificationEmailAsync(IEnumerable<ServiceProviderChanged> changes, string smtpUsername, string smtpPassword, string connectionString)
        {
            string changedAsJson = string.Empty;

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
                Subject = $"[Ingest] {changes.Count()} phone numbers changed Service Providers",
                MessageBody = output.ToString()
            };

            return await notificationEmail.SendEmailAsync(smtpUsername, smtpPassword).ConfigureAwait(false);
        }
    }
}