using NumberSearch.DataAccess.Models;

using Serilog;

using ServiceReference1;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FirstCom
{
    public sealed class NpaNxxFirstPointCom
    {
        public static async Task<PhoneNumber[]> GetAsync(ReadOnlyMemory<char> npa, ReadOnlyMemory<char> nxx, ReadOnlyMemory<char> did, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            var Auth = new Credentials
            {
                Username = username.ToString(),
                Password = password.ToString()
            };
            var DIDSearch = new DIDOrderQuery
            {
                DID = did.ToString(),
                NPA = npa.ToString(),
                NXX = nxx.ToString(),
                RateCenter = string.Empty
            };
            // Limited to 100 results at the moment. There's no way to offset the results to get the complete list of numbers, so we won't bother.
            int ReturnAmount = 100;

            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            var list = new List<PhoneNumber>();

            var result = await client.DIDInventorySearchAsync(Auth, DIDSearch, ReturnAmount).ConfigureAwait(false);

            foreach (var item in result.DIDOrder)
            {
                if (item.DID.StartsWith('1'))
                {
                    var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.DID[1..].AsSpan(), out var phoneNumber);

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
            return [..list];
        }
    }
}
