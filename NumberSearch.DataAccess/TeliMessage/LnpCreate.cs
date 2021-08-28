using Flurl.Http;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.TeleMesssage
{
    public class LnpCreate
    {
        public int code { get; set; }
        public string status { get; set; }
        public LnpCreateResponse data { get; set; }

        public class LnpCreateResponse
        {
            public string id { get; set; }
            public string ticket_id { get; set; }
            public string btn { get; set; }
            public string location_type { get; set; }
            public string business_contact { get; set; }
            public string business_name { get; set; }
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string account_number { get; set; }
            public string service_address { get; set; }
            public string service_city { get; set; }
            public string service_state { get; set; }
            public string service_zip { get; set; }
            public int partial_port { get; set; }
            public string partial_port_details { get; set; }
            public int wireless_number { get; set; }
            public string wireless_pin { get; set; }
            public string caller_id { get; set; }
            public string foc_date { get; set; }
            public string signature_s3_filename { get; set; }
            public string bill_s3_filename { get; set; }
            public LnpNumberResponse[] numbers { get; set; }
        }

        public class LnpNumberResponse
        {
            public string lnp_request_id { get; set; }
            public string internal_order_id { get; set; }
            public string number { get; set; }
            public string state { get; set; }
            public string ratecenter { get; set; }
            public string preferred_carrier { get; set; }
            public DateTime? foc_date { get; set; }
            public string port_completed { get; set; }
            public string call_flow_id { get; set; }
            public DateTime? create_dt { get; set; }
            public DateTime? modify_dt { get; set; }
        }

        public static async Task<LnpCreate> GetAsync(PortRequest PortRequest, IEnumerable<PortedPhoneNumber> PhoneNumbers, Guid token)
        {
            string baseUrl = "https://apiv1.teleapi.net/";
            string endpoint = "lnp/create";
            string tokenParameter = $"?token={token}";
            string numbersParameter = $"&numbers=";
            foreach (var number in PhoneNumbers)
            {
                // The last number should not have a comma.
                if (number.PortedDialedNumber == PhoneNumbers.LastOrDefault().PortedDialedNumber)
                {
                    numbersParameter += number.PortedDialedNumber;
                }
                else
                {
                    numbersParameter += $"{number.PortedDialedNumber},";
                }
            }
            string btnParameter = $"&btn={PortRequest.BillingPhone.Replace(" ", "").Trim()}";
            string locationTypeParameter = $"&location_type={PortRequest.LocationType.ToLowerInvariant()}";
            string businessContactParameter = $"&business_contact={PortRequest.BusinessContact.Trim()}";
            string businessNameParameter = $"&business_name={PortRequest.BusinessName.Trim()}";
            string firstNameParameter = $"&first_name={PortRequest.ResidentialFirstName.Trim()}";
            string lastNameParameter = $"&last_name={PortRequest.ResidentialLastName.Trim()}";
            string accountNumberParameter = $"&account_number={PortRequest.ProviderAccountNumber.Trim()}";
            string serviceAddressParameter = $"&service_address={PortRequest.Address} {PortRequest.Address2}";
            string serviceCityParameter = $"&service_city={PortRequest.City}";
            string serviceStateParameter = $"&service_state={PortRequest.State}";
            string serviceZipParameter = $"&service_zip={PortRequest.Zip}";
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(PortRequest.BusinessContact) ? $"{PortRequest.ResidentialFirstName} {PortRequest.ResidentialLastName}" : PortRequest.BusinessContact);
            var base64Signature = Convert.ToBase64String(plainTextBytes);
            string signatureParameter = $"&signature={base64Signature}";
            string partialPortParameter = $"&partial_port=";
            partialPortParameter += PortRequest.PartialPort ? "1" : "0";
            string partialPortDetailsParameter = $"&partial_port_details={PortRequest.PartialPortDescription}";
            string wirelessNumberParameter = $"&wireless_number=";
            wirelessNumberParameter += PortRequest.WirelessNumber ? "1" : "0";
            string wirelessPinParameter = $"&wireless_pin={PortRequest.WirelessNumber}";
            string callerIdParameter = $"&caller_id={PortRequest.CallerId.Trim()}";
            string billFileParameter = $"&bill_file=";
            string billNameParameter = $"&bill_name={PortRequest.BillImagePath}";
            string url = $"{baseUrl}{endpoint}{tokenParameter}{numbersParameter}{btnParameter}{locationTypeParameter}{businessContactParameter}{businessNameParameter}{firstNameParameter}{lastNameParameter}{accountNumberParameter}{serviceAddressParameter}{serviceCityParameter}{serviceStateParameter}{serviceZipParameter}{signatureParameter}{partialPortParameter}{partialPortDetailsParameter}{wirelessNumberParameter}{wirelessPinParameter}{callerIdParameter}{billFileParameter}{billNameParameter}";
            return await url.GetJsonAsync<LnpCreate>().ConfigureAwait(false);
        }
    }
}