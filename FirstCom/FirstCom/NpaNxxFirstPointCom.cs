using NumberSearch.DataAccess;

using Serilog;

using ServiceReference1;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirstCom
{
    public sealed class NpaNxxFirstPointCom
    {
        public static async Task<IEnumerable<PhoneNumber>> GetAsync(string npa, string nxx, string did, string username, string password)
        {
            var Auth = new Credentials
            {
                Username = username,
                Password = password
            };
            var DIDSearch = new DIDOrderQuery
            {
                DID = did,
                NPA = npa,
                NXX = nxx,
                RateCenter = string.Empty
            };
            var ReturnAmount = 1000;


            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            var result = await client.DIDInventorySearchAsync(Auth, DIDSearch, ReturnAmount).ConfigureAwait(false);

            var list = new List<PhoneNumber>();

            foreach (var item in result.DIDOrder)
            {
                if (item.DID.StartsWith('1'))
                {
                    var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(item.DID[1..], out var phoneNumber);

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

            return list;
        }
    }
}
