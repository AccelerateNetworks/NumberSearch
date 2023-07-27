using ServiceReference1;

using System.Threading.Tasks;

namespace FirstCom
{
    public class FirstPointComSMS
    {
        /// <summary>
        /// Gets the existing SMS routing plan with FirstPointCom for a given dialed number.
        /// </summary>
        /// <param name="dialedNumber">Must lead the dialed number with a 1.</param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static async Task<SMSLongcodeRoute> GetSMSRoutingByDialedNumberAsync(string dialedNumber, string username, string password)
        {
            var Auth = new Credentials
            {
                Username = username,
                Password = password
            };

            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            return await client.LongCodeShowRoutingAsync(Auth, dialedNumber).ConfigureAwait(false);
        }

        public static async Task<QueryResult> EnableSMSByDialedNumberAsync (string dialedNumber, string username, string password)
        {
            var Auth = new Credentials
            {
                Username = username,
                Password = password
            };

            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            return await client.DIDSMSEnableAsync(Auth, dialedNumber).ConfigureAwait(false);
        }

        public static async Task<QueryResult> RouteSMSToEPIDByDialedNumberAsync(string dialedNumber, int EPID, string username, string password)
        {
            var Auth = new Credentials
            {
                Username = username,
                Password = password
            };

            using var client = new DIDManagementSoapClient(DIDManagementSoapClient.EndpointConfiguration.DIDManagementSoap);

            return await client.DIDRouteSMSToEPIDBasicAsync(Auth, dialedNumber, EPID).ConfigureAwait(false);
        }
    }
}