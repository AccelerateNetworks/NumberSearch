using ServiceReference;

using System.Threading.Tasks;

namespace FirstCom
{
    public class FirstPointComOrderPhoneNumber
    {
        public static async Task<QueryResult> PostAsync(string dialedNumber, string username, string password)
        {
            var Auth = new Credentials
            {
                Username = username,
                Password = password
            };

            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            return await client.DIDOrderAsync(Auth, dialedNumber, false).ConfigureAwait(false);
        }
    }
}