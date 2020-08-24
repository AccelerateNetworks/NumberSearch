using ServiceReference;

using System.Threading.Tasks;

namespace BulkVS
{
    public class BulkVSOrderPhoneNumber
    {
        public static async Task<DnOrderResponse> GetAsync(string dialedNumber, string trunkGroup, string cnamLookup, string lidb, string messaging, string portoutpin, string apiKey, string apiSecret)
        {
            using var client = new bulkvsPortClient(bulkvsPortClient.EndpointConfiguration.bulkvsPort);

            var result = await client.DnOrderAsync(apiKey, apiSecret, dialedNumber, trunkGroup, cnamLookup, lidb, messaging, portoutpin).ConfigureAwait(false);

            return result;
        }
    }
}