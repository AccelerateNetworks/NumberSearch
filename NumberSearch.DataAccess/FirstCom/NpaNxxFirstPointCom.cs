using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public sealed class NpaNxxFirstPointCom
    {
        public static async Task<IEnumerable<PhoneNumber>> GetAsync(string npa, string nxx, string did, string username, string password)
        {
            var Auth = new FirstCom.Credentials
            {
                Username = username,
                Password = password
            };
            var DIDSearch = new FirstCom.DIDOrderQuery
            {
                DID = did,
                NPA = npa,
                NXX = nxx,
                RateCenter = string.Empty
            };
            var ReturnAmount = 1000;

            using var client = new FirstCom.DIDManagementSoapClient(FirstCom.DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            var result = await client.DIDInventorySearchAsync(Auth, DIDSearch, ReturnAmount).ConfigureAwait(false);

            var list = new List<PhoneNumber>();

            foreach (var item in result.DIDOrder)
            {
                bool checkNpa = int.TryParse(item.NPA, out int outNpa);
                bool checkNxx = int.TryParse(item.NXX, out int outNxx);
                bool checkXxxx = int.TryParse(item.DID.Substring(7), out int outXxxx);

                if (checkNpa && outNpa < 1000 && checkNxx && outNxx < 1000 && checkXxxx && outXxxx < 10000 && item.DID.Length == 11)
                {
                    list.Add(new PhoneNumber
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

            return list;
        }
    }
}
