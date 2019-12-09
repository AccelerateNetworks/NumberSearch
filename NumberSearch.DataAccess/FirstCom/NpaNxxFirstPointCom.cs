using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public sealed class NpaNxxFirstPointCom
    {
        public static async Task<IEnumerable<PhoneNumber>> GetAsync(string? npa, string? nxx, string? did, string username, string password)
        {
            var request = new FirstCom.DIDInventorySearchRequest
            {
                Auth = new FirstCom.Credentials
                {
                    Username = username,
                    Password = password
                },
                DIDSearch = new FirstCom.DIDOrderQuery
                {
                    DID = did ?? string.Empty,
                    NPA = npa ?? string.Empty,
                    NXX = nxx ?? string.Empty,
                    RateCenter = string.Empty
                },
                ReturnAmount = 1000
            };

            using var client = new FirstCom.DIDManagementSoapClient(FirstCom.DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            var result = await client.DIDInventorySearchAsync(request);

            var list = new List<PhoneNumber>();

            foreach (var item in result.DIDInventorySearchResult.DIDOrder)
            {
                bool checkNpa = int.TryParse(item.NPA, out int outNpa);
                bool checkNxx = int.TryParse(item.NXX, out int outNxx);
                bool checkXxxx = int.TryParse(item.DID.Substring(7), out int outXxxx);

                if (checkNpa && checkNxx && checkXxxx)
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

            }

            return list;
        }
    }
}
