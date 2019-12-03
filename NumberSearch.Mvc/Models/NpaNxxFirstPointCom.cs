using DIDManagement;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Models
{
    public class NpaNxxFirstPointCom
    {
        public static async Task<IEnumerable<PhoneNumber>> GetAsync(string? npa, string? nxx, string? did, string username, string password)
        {
            var request = new DIDManagement.DIDInventorySearchRequest
            {
                Auth = new DIDManagement.Credentials
                {
                    Username = username,
                    Password = password
                },
                DIDSearch = new DIDManagement.DIDOrderQuery
                {
                    DID = did ?? string.Empty,
                    NPA = npa ?? string.Empty,
                    NXX = nxx ?? string.Empty,
                    RateCenter = string.Empty
                },
                ReturnAmount = 1000
            };

            var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            var result = await client.DIDInventorySearchAsync(request);

            var list = new List<PhoneNumber>();

            foreach (var item in result.DIDInventorySearchResult.DIDOrder)
            {
                list.Add(new PhoneNumber
                {
                    NPA = item.NPA,
                    NXX = item.NXX,
                    XXXX = item.DID.Substring(7),
                    DialedNumber = item.DID.Substring(1),
                    City = "Unknown City",
                    State = "Unknown State",
                    IngestedFrom = "FirstPointCom"
                });
            }

            return list;
        }
    }
}
