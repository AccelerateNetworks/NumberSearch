using NumberSearch.DataAccess.Models;

using Serilog;

using ServiceReference1;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirstCom.Models
{
    public class FirstPointCom
    {
        /// <summary>
        /// Gets a list of valid phone numbers that begin with an area code.
        /// </summary>
        /// <param name="username"> The FirstPointCom username. </param>
        /// <param name="password"> The FirstPointCom password. </param>
        /// <returns></returns>
        public static async Task<PhoneNumber[]> GetValidNumbersByNPAAsync(ReadOnlyMemory<char> username, ReadOnlyMemory<char> password, int[] areaCodes)
        {
            var numbers = new List<PhoneNumber>();

            foreach (var code in areaCodes)
            {
                try
                {
                    numbers.AddRange(await GetPhoneNumbersByNpaNxxAsync(code, default, string.Empty.AsMemory(), username, password));
                    Log.Information($"[FirstPointCom] Found {numbers.Count} Phone Numbers");
                }
                catch (Exception ex)
                {
                    Log.Error($"Area code {code} failed @ {DateTime.Now}: {ex.Message}");
                }
            }

            return [.. numbers];
        }

        /// <summary>
        /// Order a phone number from FirstPointCom using the dialedNumber.
        /// </summary>
        /// <param name="dialedNumber"> The dialed phone number. </param>
        /// <param name="username"> The FirstPointCom username. </param>
        /// <param name="password"> The FirstPointCom password. </param>
        /// <returns></returns>
        public static async Task<QueryResult> OrderPhoneNumberAsync(ReadOnlyMemory<char> dialedNumber, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            var Auth = new Credentials
            {
                Username = username.ToString(),
                Password = password.ToString()
            };

            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            return await client.DIDOrderAsync(Auth, dialedNumber.ToString(), false);
        }

        /// <summary>
        /// Get a list of owned phone numbers hosted by FirstPointCom.
        /// </summary>
        /// <param name="npa"> The area code of interest. </param>
        /// <param name="username"> The FirstPointCom username. </param>
        /// <param name="password"> The FirstPointCom password. </param>
        /// <returns></returns>
        public static async Task<DIDOrderInfoArray> GetOwnedPhoneNumbersAsync(int npa, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            var Auth = new Credentials
            {
                Username = username.ToString(),
                Password = password.ToString()
            };

            var DIDSearch = new DIDOrderQuery
            {
                DID = string.Empty,
                NPA = npa.ToString("000"),
                NXX = string.Empty,
                RateCenter = string.Empty
            };

            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            return await client.DIDSearchInAccountAsync(Auth, DIDSearch, 2000);
        }

        /// <summary>
        /// Gets the existing SMS routing plan with FirstPointCom for a given dialed number.
        /// </summary>
        /// <param name="dialedNumber">Must lead the dialed number with a 1.</param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static async Task<SMSLongcodeRoute> GetSMSRoutingByDialedNumberAsync(ReadOnlyMemory<char> dialedNumber, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            var Auth = new Credentials
            {
                Username = username.ToString(),
                Password = password.ToString()
            };

            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            return await client.LongCodeShowRoutingAsync(Auth, dialedNumber.ToString());
        }

        public static async Task<QueryResult> EnableSMSByDialedNumberAsync(ReadOnlyMemory<char> dialedNumber, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            var Auth = new Credentials
            {
                Username = username.ToString(),
                Password = password.ToString()
            };

            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            return await client.DIDSMSEnableAsync(Auth, dialedNumber.ToString());
        }

        public static async Task<QueryResult> RouteSMSToEPIDByDialedNumberAsync(ReadOnlyMemory<char> dialedNumber, int EPID, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            var Auth = new Credentials
            {
                Username = username.ToString(),
                Password = password.ToString()
            };

            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            return await client.DIDRouteSMSToEPIDBasicAsync(Auth, dialedNumber.ToString(), EPID);
        }

        public static async Task<QueryResult> SMSToEmailByDialedNumberAsync(ReadOnlyMemory<char> dialedNumber, ReadOnlyMemory<char> email, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            var Auth = new Credentials
            {
                Username = username.ToString(),
                Password = password.ToString()
            };

            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            return await client.DIDRouteSMSToEmailAsync(Auth, dialedNumber.ToString(), email.ToString());
        }

        public static async Task<RequestOCNLATAResponse> GetOCNLATAAsync(ReadOnlyMemory<char> dialedNumber, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            using var client = new OCNLATAdipSoapClient(OCNLATAdipSoapClient.EndpointConfiguration.OCNLATAdipSoap12);

            return await client.RequestOCNLATAAsync(username.ToString(), password.ToString(), dialedNumber.ToString(), DateTime.Now.ToShortDateString());
        }

        public static async Task<PhoneNumber[]> GetPhoneNumbersByNpaNxxAsync(int npa, int nxx, ReadOnlyMemory<char> did, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            var Auth = new Credentials
            {
                Username = username.ToString(),
                Password = password.ToString()
            };
            var DIDSearch = new DIDOrderQuery
            {
                DID = did.ToString(),
                NPA = npa.ToString("000"),
                NXX = nxx.ToString("000"),
                RateCenter = string.Empty
            };
            // There's no way to offset the results to get the complete list of numbers, so we won't bother.
            int ReturnAmount = 9999;

            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);
            DIDOrderInfoArray result = await client.DIDInventorySearchAsync(Auth, DIDSearch, ReturnAmount);

            // Supply a default capacity to the list to skip resizing it.
            List<PhoneNumber> list = new(result.DIDOrder.Length);

            Log.Information("[FirstPointCom] {@text}", result.queryresult.text);

            foreach (var item in result.DIDOrder)
            {
                if (item.DID.StartsWith('1'))
                {
                    bool checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.DID[1..].AsSpan(), out var phoneNumber);

                    if (checkParse)
                    {
                        list.Add(new PhoneNumber
                        {
                            NPA = phoneNumber.NPA,
                            NXX = phoneNumber.NXX,
                            XXXX = phoneNumber.XXXX,
                            DialedNumber = phoneNumber.DialedNumber,
                            City = "Unknown City",
                            State = "Unknown State",
                            DateIngested = DateTime.Now,
                            IngestedFrom = "FirstPointCom"
                        });
                    }
                    else
                    {
                        Log.Error($"[FirstCom] This number failed to parse {item.DID}");
                    }
                }
                else
                {
                    Log.Error($"[FirstCom] This number did not start with a 1: {item.DID}");
                }
            }
            return [.. list];
        }

    }
}
