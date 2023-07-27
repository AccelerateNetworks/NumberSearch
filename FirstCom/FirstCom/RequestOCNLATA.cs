using ServiceReference1;

using System;
using System.Threading.Tasks;

namespace FirstCom
{
    public class RequestOCNLATA
    {
        public static async Task<RequestOCNLATAResponse> GetAsync(string dialedNumber, string username, string password)
        {
            var Auth = new Credentials
            {
                Username = username,
                Password = password
            };

            using var client = new OCNLATAdipSoapClient(OCNLATAdipSoapClient.EndpointConfiguration.OCNLATAdipSoap12);

            return await client.RequestOCNLATAAsync(username, password, dialedNumber, DateTime.Now.ToShortDateString()).ConfigureAwait(false);
        }
    }
}