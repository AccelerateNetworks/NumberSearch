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
    }
}
