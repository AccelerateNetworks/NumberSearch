using ServiceReference;

using System.Threading.Tasks;

namespace FirstCom
{
    public class FirstPointComListOwnedPhoneNumber
    {
        public static async Task<DIDOrderInfoArray> GetAllAsync(string npa, string username, string password)
        {
            var Auth = new Credentials
            {
                Username = username,
                Password = password
            };

            var DIDSearch = new DIDOrderQuery
            {
                DID = string.Empty,
                NPA = npa,
                NXX = string.Empty,
                RateCenter = string.Empty
            };

            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            return await client.DIDSearchInAccountAsync(Auth, DIDSearch, 2000).ConfigureAwait(false);
        }
    }
}