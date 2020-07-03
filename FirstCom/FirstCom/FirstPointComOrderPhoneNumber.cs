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

        public static async Task<QueryResult> SetGateway(string dialedNumber, string gatewayIP, string username, string password)
        {
            var Auth = new Credentials
            {
                Username = username,
                Password = password
            };

            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            return await client.DIDRouteVoiceToGatewayBasicAsync(Auth, dialedNumber, gatewayIP).ConfigureAwait(false);
        }
    }
}