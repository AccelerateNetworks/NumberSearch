using ServiceReference;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace BulkVS.BulkVS
{
    public class FirstPointComOrderPhoneNumber
    {
        public static async Task<IEnumerable<QueryResult>> PostAsync(string dialedNumber, string gatewayIP, string username, string password)
        {
            var Auth = new Credentials
            {
                Username = username,
                Password = password
            };

            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            // Return the responses from both calls.
            var results = new List<QueryResult>();

            var result = await client.DIDOrderAsync(Auth, dialedNumber, false).ConfigureAwait(false);
            results.Add(result);

            if (result.code == 200)
            {
                var gatewayResult = await client.DIDRouteVoiceToGatewayBasicAsync(Auth, dialedNumber, gatewayIP).ConfigureAwait(false);
                results.Add(gatewayResult);
            }

            return results;
        }
    }
}