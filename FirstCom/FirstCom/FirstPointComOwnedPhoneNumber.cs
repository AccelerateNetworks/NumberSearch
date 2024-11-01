using ServiceReference1;

using System;
using System.Threading.Tasks;

namespace FirstCom
{
    public class FirstPointComOwnedPhoneNumber
    {
        public static async Task<DIDOrderInfoArray> GetAsync(ReadOnlyMemory<char> npa, ReadOnlyMemory<char> username, ReadOnlyMemory<char> password)
        {
            var Auth = new Credentials
            {
                Username = username.ToString(),
                Password = password.ToString()
            };

            var DIDSearch = new DIDOrderQuery
            {
                DID = string.Empty,
                NPA = npa.ToString(),
                NXX = string.Empty,
                RateCenter = string.Empty
            };

            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            return await client.DIDSearchInAccountAsync(Auth, DIDSearch, 2000).ConfigureAwait(false);
        }
    }
}