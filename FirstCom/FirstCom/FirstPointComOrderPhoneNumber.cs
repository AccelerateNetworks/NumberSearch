using ServiceReference;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BulkVS.BulkVS
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

            var result = await client.DIDOrderAsync(Auth, dialedNumber, false).ConfigureAwait(false);

            // What do we put for the GatewayIP parameter?
            //var gatewayResult = await client.DIDRouteVoiceToGatewayBasicAsync().ConfigureAwait(false);

            return result;
        }
    }
}