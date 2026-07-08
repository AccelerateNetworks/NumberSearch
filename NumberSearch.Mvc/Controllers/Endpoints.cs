using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using nietras.SeparatedValues;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.FCC;
using NumberSearch.Mvc.Models;

using PhoneNumbersNA;

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;

using ZLinq;

namespace NumberSearch.Mvc.Controllers
{
    public static class Endpoints
    {
        /// <summary>
        /// Represents the result of a bulk number lookup operation.
        /// </summary>
        /// <param name="DialedNumber"></param>
        /// <param name="City"></param>
        /// <param name="State"></param>
        /// <param name="DateIngested"></param>
        /// <param name="Wireless"></param>
        /// <param name="Portable"></param>
        /// <param name="LastPorted"></param>
        /// <param name="SPID"></param>
        /// <param name="LATA"></param>
        /// <param name="LEC"></param>
        /// <param name="LECType"></param>
        /// <param name="LIDBName"></param>
        /// <param name="LRN"></param>
        /// <param name="OCN"></param>
        /// <param name="CarrierName"></param>
        /// <param name="CarrierLogoLink"></param>
        /// <param name="CarrierColor"></param>
        /// <param name="CarrierType"></param>
        public readonly record struct BulkLookupResult(string DialedNumber, string City, string State, DateTime DateIngested, bool Wireless, bool Portable, DateTime LastPorted, string SPID, string LATA, string LEC, string LECType, string LIDBName, string LRN, string OCN, string CarrierName, string CarrierLogoLink, string CarrierColor, string CarrierType);

        /// <summary>
        /// Performs a bulk lookup for dialed phone numbers.
        /// </summary>
        /// <param name="token">The authentication token for the request.</param>
        /// <param name="dialedNumber">A comma-separated list of dialed phone numbers to look up.</param>
        /// <param name="mvcConfiguration">The configuration for the application.</param>
        /// <returns></returns>
        public static async Task<Results<Ok<BulkLookupResult[]>, BadRequest<string>>> NumberSearchBulkAsync([Required] string token, [Required] string dialedNumber, [FromServices] MvcConfiguration mvcConfiguration)
        {
            if (!string.IsNullOrWhiteSpace(token) && token == "Memorable8142024")
            {
                // Add portable numbers to cart in bulk
                if (!string.IsNullOrWhiteSpace(dialedNumber))
                {
                    var parsedNumbers = dialedNumber.ExtractDialedNumbers().ToArray();

                    if (parsedNumbers.Length == 0)
                    {
                        return TypedResults.BadRequest("No dialed phone numbers found. Please try a different query.");
                    }

                    var results = new ConcurrentBag<PortedPhoneNumber>();
                    await Parallel.ForEachAsync(parsedNumbers, async (number, token) =>
                    {
                        var lookup = new LookupController(mvcConfiguration);
                        var result = await lookup.VerifyPortabilityAsync(number);
                        results.Add(result);
                    });

                    var lookups = new List<BulkLookupResult>(results.Count);
                    foreach (var number in results)
                    {
                        lookups.Add(new BulkLookupResult(number.PortedDialedNumber, number.City, number.State, number.DateIngested, number.Wireless, number.Portable, number.LrnLookup.LastPorted, number.LrnLookup.SPID, number.LrnLookup.LATA, number.LrnLookup.LEC, number.LrnLookup.LECType, number.LrnLookup.LIDBName, number.LrnLookup.LRN, number.LrnLookup.OCN, number.Carrier.Name, number.Carrier.LogoLink, number.Carrier.Color, number.Carrier.Type));
                    }

                    return TypedResults.Ok(lookups.ToArray());
                }
                else
                {
                    return TypedResults.BadRequest("No dialed phone numbers found. Please try a different query.");
                }
            }
            else
            {
                return TypedResults.BadRequest("Token is invalid. Please supply the correct token in your request or contact support@acceleratenetworks.com for help.");
            }
        }

        /// <summary>
        /// Represents the speeds for a provider in a specific geographic area.
        /// </summary>
        /// <param name="geoid"></param>
        /// <param name="frn"></param>
        /// <param name="provider"></param>
        /// <param name="technology"></param>
        /// <param name="techdesc"></param>
        /// <param name="down"></param>
        /// <param name="up"></param>
        /// <param name="serviceId"></param>
        public readonly record struct ProviderGeoSpeeds(string geoid, string frn, string provider, string technology, string techdesc, decimal down, decimal up, Guid serviceId);

        /// <summary>
        /// Represents the technical description for a specific technology.
        /// </summary>
        /// <param name="code">The code for the technology.</param>
        /// <param name="name">The name of the technology.</param>
        /// <param name="description">The description of the technology.</param>
        public readonly record struct TechDesc(string code, string name, string description);

        /// <summary>
        /// Looks up geographic information for a specific state.
        /// </summary>
        /// <param name="state">The state for which to look up geographic information.</param>
        /// <param name="geoid">The geographic identifier within that state.</param>
        /// <param name="mvcConfiguration">The configuration for the application.</param>
        /// <returns>An array of ProviderGeoSpeeds.</returns>
        public static async Task<Results<Ok<ProviderGeoSpeeds[]>, BadRequest<string>>> FCCStateGeoIdLookup([Required] string state, string geoid, [FromServices] MvcConfiguration mvcConfiguration)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                return TypedResults.BadRequest("No state provided (ex: Washington). Please try a different query. 🥺👉👈");
            }

            // Add portable numbers to cart in bulk
            if (!string.IsNullOrWhiteSpace(geoid))
            {
                var canidates = new List<ProviderGeoSpeeds>();

                var techDesc = new List<TechDesc>() {
                    new("10", "Copper", "Fixed wireline service using copper wire (e.g., Asymmetric or Symmetric DSL, ethernet over copper, T-1, etc.)."),
                    new("40","Cable","Fixed wireline service using coaxial cable or hybrid fiber-coaxial (e.g., DOCSISx)."),
                    new("50","Fiber to the Premises","Fixed wireline service using fiber to the home or business end user, but does not include \"fiber to the curb\"."),
                    new("70","Unlicensed Fixed Wireless","Fixed terrestrial wireless service using entirely unlicensed spectrum, including services provided over WiFi as a fixed solution."),
                    new("71","Licensed Fixed Wireless","Fixed wireless service using entirely licensed spectrum (including priority access licenses in the 3.5 GHz band) or a hybrid of licensed, unlicensed, and licensed-by-rule spectrum to make last-mile connections to fixed locations. This includes service provided over a 4G LTE or 5G-NR mobile network but sold as a fixed solution."),
                    new("72","LBR Fixed Wireless","Fixed wireless services using entirely licensed-by-rule spectrum or a hybrid of licensed-by-rule and unlicensed spectrum to make last-mile connections to fixed locations. Licensed-by-rule spectrum users include operators providing last-mile connections through general authorized access (GAA) in the 3.5 GHz Citizens Broadband Radio Service (CBRS) band."),};

                var result = await ListAsOfDates.GetAsync(mvcConfiguration.FCCUsername.AsMemory(), mvcConfiguration.FCCAPIToken.AsMemory());
                var date = result.data.AsValueEnumerable().OrderByDescending(x => x.as_of_date).Where(x => x.data_type is "availability").FirstOrDefault();
                var listing = await ListAvailabilityData.GetAsync(date.as_of_date.AsMemory(), mvcConfiguration.FCCUsername.AsMemory(), mvcConfiguration.FCCAPIToken.AsMemory());
                var toGet = listing.data.Where(x => x.state_name.Equals(state, StringComparison.InvariantCultureIgnoreCase)).Where(x => x.technology_code is not "60" && x.technology_code is not "61");
                string downloadsPath = Path.GetTempPath();
                var services = await Service.GetAllAsync(mvcConfiguration.PostgresqlProd);
                var toLoop = toGet.ToArray();
                foreach (var item in toLoop)
                {
                    var files = Directory.GetFiles(downloadsPath);
                    var file = files.FirstOrDefault(x => x.Contains(item.file_name) && x.EndsWith(".csv"));

                    // Download and unzip, if required.
                    if (string.IsNullOrWhiteSpace(file))
                    {
                        string filePath = await item.DownloadFileAsync(downloadsPath, mvcConfiguration.FCCUsername.AsMemory(), mvcConfiguration.FCCAPIToken.AsMemory());
                        await ZipFile.ExtractToDirectoryAsync(filePath, downloadsPath);
                        System.IO.File.Delete(filePath);
                        files = Directory.GetFiles(downloadsPath);
                        file = files.FirstOrDefault(x => x.Contains(item.file_name) && x.EndsWith(".csv"));
                    }

                    if (!string.IsNullOrWhiteSpace(file))
                    {
                        using var reader = Sep.Reader().FromFile(file);
                        foreach (var readRow in reader)
                        {
                            if (MemoryExtensions.Equals(readRow["block_geoid"].Span, geoid.AsSpan(), StringComparison.Ordinal))
                            {
                                var id = readRow["block_geoid"].ToString();
                                var frn = readRow["frn"].ToString();
                                var provider = readRow["brand_name"].ToString();
                                var down = readRow["max_advertised_download_speed"].Parse<decimal>();
                                var up = readRow["max_advertised_upload_speed"].Parse<decimal>();
                                var desc = techDesc.FirstOrDefault(x => x.code == item.technology_code);
                                var service = services.FirstOrDefault(x => x.Name == item.technology_code_desc);
                                canidates.Add(new ProviderGeoSpeeds(id, frn, provider, item.technology_code_desc, desc.description, down, up, service.ServiceId));
                            }
                        }
                    }
                }

                var providers = canidates.AsValueEnumerable().Select(x => x.provider).Distinct();
                var results = new List<ProviderGeoSpeeds>();
                var quantumPresent = providers.Any(x => x is "Quantum Fiber");
                foreach (var p in providers)
                {
                    var winner = canidates.AsValueEnumerable().Where(x => x.provider == p).MaxBy(x => x.up);
                    if (!(winner.provider is "CenturyLink" && quantumPresent))
                    {
                        results.Add(winner);
                    }
                }

                var techs = results.AsValueEnumerable().Select(x => x.technology).Distinct();
                var singlePerTech = new List<ProviderGeoSpeeds>();

                foreach (var p in techs)
                {
                    var speedWinner = canidates.AsValueEnumerable().Where(x => x.technology == p).MaxBy(x => x.up);
                    singlePerTech.Add(speedWinner);
                }

                return TypedResults.Ok(singlePerTech.AsValueEnumerable().Where(x => x.up > 0).OrderByDescending(x => x.down).ToArray());
            }
            else
            {
                return TypedResults.BadRequest("No geoid provided (ex: 530330060001014). Please try a different query. 🥺👉👈");
            }
        }

        /// <summary>
        /// https://www.twilio.com/docs/lookup/v2-api
        /// </summary>
        /// <param name="calling_country_code">International dialing prefix of the phone number defined in the E.164 standard.</param>
        /// <param name="country_code">The phone number's ISO country code.</param>
        /// <param name="phone_number">The phone number in E.164 format, which consists of a + followed by the country code and subscriber number.</param>
        /// <param name="national_format">The phone number in national format.</param>
        /// <param name="valid">Boolean which indicates if the phone number is in a valid range that can be freely assigned by a carrier to a user.</param>
        /// <param name="validation_errors">Contains reasons why a phone number is invalid. Possible values: TOO_SHORT, TOO_LONG, INVALID_BUT_POSSIBLE, INVALID_COUNTRY_CODE, INVALID_LENGTH, NOT_A_NUMBER.</param>
        /// <param name="caller_name">An object that contains caller name information based on CNAM.</param>
        /// <param name="sim_swap">An object that contains information on the last date the subscriber identity module (SIM) was changed for a mobile phone number.</param>
        /// <param name="call_forwarding">An object that contains information on the unconditional call forwarding status of mobile phone number.</param>
        /// <param name="line_status">An object that contains line status information for a mobile phone number.</param>
        /// <param name="line_type_intelligence">An object that contains line type information including the carrier name, mobile country code, and mobile network code.</param>
        /// <param name="identity_match">An object that contains identity match information. The result of comparing user-provided information including name, address, date of birth, national ID, against authoritative phone-based data sources.</param>
        /// <param name="reassigned_number">An object that contains reassigned number information. Reassigned Numbers will return a phone number's reassignment status given a phone number and date.</param>
        /// <param name="sms_pumping_risk">An object that contains information on if a phone number has been currently or previously blocked by Verify Fraud Guard for receiving malicious SMS pumping traffic as well as other signals associated with risky carriers and low conversion rates.</param>
        /// <param name="phone_number_quality_score">An object that contains information of a mobile phone number quality score. Quality score will return a risk score about the phone number.</param>
        /// <param name="pre_fill">An object that contains pre fill information. pre_fill will return PII information associated with the phone number like first name, last name, address line, country code, state and postal code.</param>
        /// <param name="url">The absolute URL of the resource.</param>
        public readonly record struct NumberLookupResponse(
             string calling_country_code,
             string country_code,
             string phone_number,
             string national_format,
             bool valid,
             string[] validation_errors,
             CallerName? caller_name,
             SimSwap? sim_swap,
             CallForwarding? call_forwarding,
             LineStatus? line_status,
             LineTypeIntelligence? line_type_intelligence,
             IdentityMatch? identity_match,
             ReassignedNumber? reassigned_number,
             SmsPumpingRisk? sms_pumping_risk,
             object? phone_number_quality_score,
             object? pre_fill,
             string url
        );

        /// <summary>
        /// https://www.twilio.com/docs/lookup/v2-api/line-type-intelligence
        /// </summary>
        /// <param name="mobile_country_code">The three-digit mobile country code of the carrier, used with the mobile_network_code to identify a mobile network operator.</param>
        /// <param name="mobile_network_code">The two- or three-digit mobile network code of the carrier, used with the mobile country code to identify a mobile network operator. This is only returned for mobile numbers.</param>
        /// <param name="carrier_name">The name of the carrier.</param>
        /// <param name="type">	The phone number type.</param>
        /// <param name="error_code">The error code. If there's no error, this value will be null.</param>
        public readonly record struct LineTypeIntelligence(string mobile_country_code, string mobile_network_code, string carrier_name, string type, int? error_code);
        /// <summary>
        /// https://www.twilio.com/docs/lookup/v2-api/caller-name
        /// </summary>
        /// <param name="caller_name">The name of the owner of the phone number. If not available, this will be null.</param>
        /// <param name="caller_type">The caller type. Possible values are BUSINESS and CONSUMER. If not available, this will be null.</param>
        /// <param name="error_code">The error code, if any, associated with your request.</param>
        public readonly record struct CallerName(string caller_name, string caller_type, int? error_code);
        public readonly record struct SimSwap(LastSimSwap last_sim_swap, string carrier_name, string mobile_country_code, string mobile_network_code, int error_code);
        public readonly record struct LastSimSwap(DateTime last_sim_swap_date, string swapped_period, bool swapped_in_period);
        public readonly record struct CallForwarding(bool call_forwarding_enabled, int error_code);
        public readonly record struct ReassignedNumber(string is_number_reassigned, int error_code);
        public readonly record struct LineStatus(string status, int error_code);
        public readonly record struct IdentityMatch(bool identity_match, int error_code);
        /// <summary>
        /// https://www.twilio.com/docs/lookup/v2-api/sms-pumping-risk
        /// </summary>
        /// <param name="carrier_risk_category">The risk category of the carrier based on its score. Available values are high, moderate, mild, and low.</param>
        /// <param name="number_blocked">A Boolean indicating whether the phone number is currently blocked by Verify Fraud Guard for receiving malicious SMS pumping traffic.</param>
        /// <param name="number_blocked_date">The most recent date the phone number was blocked by Verify Fraud Guard. Returns null if the phone number has never been blocked or processed by Verify Fraud Guard.</param>
        /// <param name="number_blocked_last_3_months">A Boolean indicating whether the phone number has been blocked by Verify Fraud Guard in the last three months. Returns null if the phone number has never been processed by Verify Fraud Guard.</param>
        /// <param name="sms_pumping_risk_score">The risk score for the phone number, calculated from patterns in messaging traffic. Ranges from 0 (no risk) to 100 (high risk).</param>
        /// <param name="error_code">The error code, if any, associated with your request.</param>
        public readonly record struct SmsPumpingRisk(string carrier_risk_category, bool number_blocked, DateTime? number_blocked_date, bool? number_blocked_last_3_months, int sms_pumping_risk_score, int error_code);

        public static async Task<Results<Ok<NumberLookupResponse>, BadRequest<string>>> LookupAPIV2([Required] string PhoneNumber, string? Fields, [FromServices] MvcConfiguration mvcConfiguration)
        {
            if (string.IsNullOrWhiteSpace(Fields))
            {
                return TypedResults.BadRequest("Fields parameter is required. Ex: Fields=line_type_intelligence or Fields=caller_name");
            }

            string[] fieldsArray = Fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (Fields is "line_type_intelligence")
            {
                return TypedResults.Ok(new NumberLookupResponse(
                    calling_country_code: "+1",
                    country_code: "US",
                    phone_number: PhoneNumber,
                    national_format: "(555) 555-5555",
                    valid: true,
                    validation_errors: Array.Empty<string>(),
                    caller_name: null,
                    sim_swap: null,
                    call_forwarding: null,
                    line_status: null,
                    line_type_intelligence: new LineTypeIntelligence("310", "260", "T-Mobile", "mobile", null),
                    identity_match: null,
                    reassigned_number: null,
                    sms_pumping_risk: null,
                    phone_number_quality_score: null,
                    pre_fill: null,
                    url: $"https://api.example.com/lookup/v2/phone_numbers/{PhoneNumber}?fields=line_type_intelligence"
                ));
            }
            else if (Fields is "caller_name")
            {
                return TypedResults.Ok(new NumberLookupResponse(
                    calling_country_code: "+1",
                    country_code: "US",
                    phone_number: PhoneNumber,
                    national_format: "(555) 555-5555",
                    valid: true,
                    validation_errors: Array.Empty<string>(),
                    caller_name: new CallerName("John Doe", "CONSUMER", null),
                    sim_swap: null,
                    call_forwarding: null,
                    line_status: null,
                    line_type_intelligence: null,
                    identity_match: null,
                    reassigned_number: null,
                    sms_pumping_risk: null,
                    phone_number_quality_score: null,
                    pre_fill: null,
                    url: $"https://api.example.com/lookup/v2/phone_numbers/{PhoneNumber}?fields=caller_name"
                ));
            }
            else if (Fields is "sim_swap")
            {
                return TypedResults.BadRequest("Fields sim_swap is not supported.");
            }
            else if (Fields is "call_forwarding")
            {
                return TypedResults.BadRequest("Fields call_forwarding is not supported.");
            }
            else if (Fields is "line_status")
            {
                return TypedResults.BadRequest("Fields line_status is not supported.");
            }
            else if (Fields is "identity_match")
            {
                return TypedResults.BadRequest("Fields identity_match is not supported.");
            }
            else if (Fields is "reassigned_number")
            {
                return TypedResults.BadRequest("Fields reassigned_number is not supported.");
            }
            else if (Fields is "sms_pumping_risk")
            {
                return TypedResults.BadRequest("Fields sms_pumping_risk is not supported.");
            }
            else
            {
                return TypedResults.BadRequest("Invalid Fields parameter. Please specify a valid field to look up.");
            }
        }
    }
}
