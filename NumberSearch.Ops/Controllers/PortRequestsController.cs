using AccelerateNetworks.Operations;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Flurl.Http;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess.BulkVS;
using NumberSearch.Ops.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public partial class PortRequestsController(IConfiguration config, numberSearchContext context) : Controller
{
    private readonly string _azureStorage = config.GetConnectionString("AzureStorageAccount") ?? string.Empty;
    private readonly string _bulkVSAPIKey = config.GetConnectionString("BulkVSAPIKEY") ?? string.Empty;
    private readonly string _bulkVSusername = config.GetConnectionString("BulkVSUsername") ?? string.Empty;
    private readonly string _bulkVSpassword = config.GetConnectionString("BulkVSPassword") ?? string.Empty;
    private readonly string _emailOrders = config.GetConnectionString("EmailOrders") ?? string.Empty;

    public async Task<AccelerateNetworks.Operations.PortedPhoneNumber> VerifyPortabilityAsync(string number)
    {
        var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(number, out var phoneNumber);

        if (checkParse && phoneNumber is not null)
        {
            try
            {
                var portable = await ValidatePortability.GetAsync(phoneNumber.DialedNumber.AsMemory(), _bulkVSusername.AsMemory(), _bulkVSpassword.AsMemory());

                // Fail fast
                if (portable.Portable is false)
                {
                    Log.Information($"[Portability] {phoneNumber.DialedNumber} is not Portable.");

                    return new AccelerateNetworks.Operations.PortedPhoneNumber
                    {
                        PortedDialedNumber = number,
                        Portable = false
                    };
                }

                // Lookup the number.
                var checkNumber = await LrnBulkCnam.GetAsync(phoneNumber.DialedNumber.AsMemory(), _bulkVSAPIKey.AsMemory());

                // Determine if the number is a wireless number.
                bool wireless = false;

                switch (checkNumber.lectype)
                {
                    case "WIRELESS":
                        wireless = true;
                        break;
                    case "PCS":
                        wireless = true;
                        break;
                    case "P RESELLER":
                        wireless = true;
                        break;
                    case "Wireless":
                        wireless = true;
                        break;
                    case "W RESELLER":
                        wireless = true;
                        break;
                    default:
                        break;
                }

                var numberName = await CnamBulkVs.GetAsync(phoneNumber.DialedNumber.AsMemory(), _bulkVSAPIKey.AsMemory());
                checkNumber = checkNumber with { LIDBName = string.IsNullOrWhiteSpace(numberName.name) ? string.Empty : numberName.name };

                Log.Information($"[Portability] {phoneNumber.DialedNumber} is Portable.");

                var portableNumber = new AccelerateNetworks.Operations.PortedPhoneNumber
                {
                    PortedPhoneNumberId = Guid.NewGuid(),
                    PortedDialedNumber = phoneNumber.DialedNumber!,
                    NPA = phoneNumber.NPA,
                    NXX = phoneNumber.NXX,
                    XXXX = phoneNumber.XXXX,
                    City = checkNumber.city,
                    State = checkNumber.province,
                    DateIngested = DateTime.Now,
                    IngestedFrom = "UserInput",
                    Wireless = wireless,
                    LrnLookup = new AccelerateNetworks.Operations.PhoneNumberLookup(checkNumber),
                    Portable = true
                };

                return portableNumber;
            }
            catch (Exception ex)
            {
                Log.Information($"[Portability] {number} is not Portable.");
                Log.Fatal($"[Portability] {ex.Message}");
                Log.Fatal($"[Portability] {ex.InnerException}");

                return new AccelerateNetworks.Operations.PortedPhoneNumber
                {
                    PortedDialedNumber = number,
                    Portable = false
                };
            }
        }
        else
        {
            Log.Information($"[Portability] {number} is not Portable. Failed NPA, NXX, XXXX parsing.");

            return new AccelerateNetworks.Operations.PortedPhoneNumber
            {
                PortedDialedNumber = number,
                Portable = false
            };
        }
    }


    [Authorize]
    [Route("/Home/PortRequests")]
    [Route("/Home/PortRequests/{orderId}")]
    public async Task<IActionResult> PortRequests(Guid? orderId)
    {
        if (orderId is not null && orderId.HasValue)
        {
            var order = await context.Orders.Where(x => x.OrderId == orderId).AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId);
            if (order is not null)
            {
                var portRequest = await context.PortRequests.Where(x => x.OrderId == order.OrderId).AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId);
                if (portRequest is not null)
                {
                    var numbers = await context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToArrayAsync();

                    return View("PortRequestEdit", new PortRequestResult
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = numbers
                    });
                }
            }
        }

        // Show all orders
        var portRequests = await context.PortRequests.OrderByDescending(x => x.DateSubmitted).Take(100).AsNoTracking().ToListAsync();

        return View("PortRequests", portRequests);
    }

    [Authorize]
    [Route("/Home/BillImage/{orderId}/")]
    public async Task<ActionResult> DownloadAsync(string orderId, string fileName)
    {
        var OrderId = new Guid(orderId);

        // Create a BlobServiceClient object which will be used to create a container client
        BlobServiceClient blobServiceClient = new(_azureStorage);

        //Create a unique name for the container
        string containerName = orderId;

        // Create the container and return a container client object
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        var files = new List<BlobItem>();

        await foreach (var item in containerClient.GetBlobsAsync())
        {
            files.Add(item);
        }

        var billImage = files.FirstOrDefault(x => x.Name == fileName);

        if (billImage is null)
        {
            return View("PortRequestEdit", new PortRequestResult
            {
                Order = await context.Orders.FirstOrDefaultAsync(x => x.OrderId == OrderId) ?? new(),
                PortRequest = await context.PortRequests.FirstOrDefaultAsync(x => x.OrderId == OrderId) ?? new(),
                PhoneNumbers = await context.PortedPhoneNumbers.Where(x => x.OrderId == OrderId).ToArrayAsync(),
                Message = $"❌ Couldn't find the bill image {fileName} for Order {orderId}."
            });
        }

        var blobClient = containerClient.GetBlobClient(billImage?.Name);
        var download = await blobClient.DownloadAsync();
        var fileBytes = Array.Empty<byte>();
        using var downloadFileStream = new MemoryStream();
        await download.Value.Content.CopyToAsync(downloadFileStream);
        fileBytes = downloadFileStream.ToArray();


        return new FileContentResult(fileBytes, download.Value.ContentType)
        {
            FileDownloadName = billImage?.Name
        };
    }

    [Authorize]
    [Route("/Home/PortRequest/{orderId}/Delete")]
    public async Task<IActionResult> PortRequestDelete(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            return Redirect("/Home/PortRequests");
        }
        else
        {
            var portrequest = await context.PortRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrderId == Guid.Parse(orderId));

            if (portrequest is not null && portrequest.OrderId == Guid.Parse(orderId))
            {
                context.PortRequests.Remove(portrequest);
                await context.SaveChangesAsync();
            }

            return Redirect("/Home/PortRequests");
        }
    }

    [Authorize]
    [Route("/Home/PortRequests/{orderId}/{dialedNumber}")]
    public async Task<IActionResult> RemovePortedPhoneNumber(Guid? orderId, string dialedNumber)
    {
        if (!string.IsNullOrWhiteSpace(dialedNumber))
        {
            var order = await context.Orders.FirstOrDefaultAsync(x => x.OrderId == orderId);
            if (order is not null)
            {
                var portRequest = await context.PortRequests.FirstOrDefaultAsync(x => x.OrderId == order.OrderId);
                if (portRequest is not null)
                {
                    var numberToRemove = await context.PortedPhoneNumbers
                                .FirstOrDefaultAsync(x => x.OrderId == order.OrderId && x.PortedDialedNumber == dialedNumber);

                    if (numberToRemove is not null)
                    {
                        context.PortedPhoneNumbers.Remove(numberToRemove);
                        await context.SaveChangesAsync();

                        var productOrder = await context.ProductOrders
                                .FirstOrDefaultAsync(x => x.OrderId == order.OrderId && x.PortedPhoneNumberId == numberToRemove.PortedPhoneNumberId);

                        if (productOrder is not null)
                        {
                            context.ProductOrders.Remove(productOrder);
                            await context.SaveChangesAsync();
                        }
                    }

                    var numbers = await context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToArrayAsync();

                    return View("PortRequestEdit", new PortRequestResult
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = numbers
                    });
                }
            }
        }

        return Redirect("/Home/PortRequests");
    }

    [Authorize]
    [HttpPost("/Home/PortRequests/{orderId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PortRequestUpdate(PortRequestResult result, Guid? orderId, string dialedNumber)
    {
        var portRequest = result?.PortRequest ?? null;

        if (!string.IsNullOrWhiteSpace(dialedNumber) && orderId is not null && orderId != Guid.Empty)
        {
            var order = await context.Orders.Where(x => x.OrderId == orderId).FirstOrDefaultAsync();

            if (order is not null)
            {
                portRequest = await context.PortRequests.Where(x => x.OrderId == order.OrderId).FirstOrDefaultAsync();
                var numbers = await context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).ToArrayAsync();

                var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(dialedNumber, out var phoneNumber);

                if (portRequest is not null && checkParse && phoneNumber is not null)
                {
                    var port = await VerifyPortabilityAsync(phoneNumber.DialedNumber ?? string.Empty);

                    if (port is not null && port.Portable)
                    {
                        Log.Information($"[Portability] {port.PortedDialedNumber} is Portable.");

                        port.OrderId = order.OrderId;
                        port.PortRequestId = portRequest.PortRequestId;

                        context.PortedPhoneNumbers.Add(port);
                        await context.SaveChangesAsync();

                        numbers = await context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).ToArrayAsync();

                        var productOrder = new AccelerateNetworks.Operations.ProductOrder
                        {
                            PortedDialedNumber = port.PortedDialedNumber,
                            PortedPhoneNumberId = port.PortedPhoneNumberId,
                            Quantity = 1,
                            CreateDate = DateTime.Now,
                            OrderId = order.OrderId,
                            ProductOrderId = Guid.NewGuid()
                        };

                        context.ProductOrders.Add(productOrder);
                        await context.SaveChangesAsync();

                        return View("PortRequestEdit", new PortRequestResult
                        {
                            Order = order,
                            PortRequest = portRequest,
                            PhoneNumbers = numbers,
                            Message = $"Successfully added Ported Phone Number {port.PortedDialedNumber}.",
                            AlertType = "alert-success"
                        });
                    }
                    else
                    {
                        return View("PortRequestEdit", new PortRequestResult
                        {
                            Order = order,
                            PortRequest = portRequest,
                            PhoneNumbers = numbers,
                            Message = $"Failed to add Ported Phone Number {port?.PortedDialedNumber}."
                        });
                    }
                }
                else
                {
                    return Redirect("/Home/PortRequests");
                }
            }
        }
        else if (orderId is not null && orderId != Guid.Empty)
        {
            var order = await context.Orders.FirstOrDefaultAsync(x => x.OrderId == orderId);

            if (order is not null && portRequest is not null)
            {
                var fromDb = await context.PortRequests.FirstOrDefaultAsync(x => x.OrderId == order.OrderId);

                if (fromDb is not null)
                {
                    // If the address has changed update it.
                    if (!string.IsNullOrWhiteSpace(portRequest.UnparsedAddress) && portRequest.UnparsedAddress != fromDb.UnparsedAddress)
                    {
                        // Format the address information
                        Log.Information($"[Checkout] Parsing address data from {portRequest.UnparsedAddress}");
                        var addressParts = portRequest.UnparsedAddress?.Split(", ") ?? [];
                        if (addressParts is not null && addressParts.Length > 4)
                        {
                            fromDb.Address = addressParts[0];
                            fromDb.City = addressParts[1];
                            fromDb.State = addressParts[2];
                            fromDb.Zip = addressParts[3];
                            fromDb.UnparsedAddress = portRequest.UnparsedAddress;
                            Log.Information($"[Checkout] Address: {fromDb.Address} City: {fromDb.City} State: {fromDb.State} Zip: {fromDb.Zip}");
                        }
                        else
                        {
                            Log.Error($"[Checkout] Failed automatic address formating.");

                            portRequest = await context.PortRequests.FirstOrDefaultAsync(x => x.OrderId == portRequest.OrderId);

                            if (portRequest is not null)
                            {
                                var numbersFailed = await context.PortedPhoneNumbers.Where(x => x.OrderId == portRequest.OrderId).ToArrayAsync();

                                return View("PortRequestEdit", new PortRequestResult
                                {
                                    Order = order,
                                    PortRequest = portRequest,
                                    PhoneNumbers = numbersFailed,
                                    Message = "Failed to update this Port Request. 😠 The address could not be parsed, please file a bug on Github.",
                                    AlertType = "alert-danger"
                                });
                            }
                        }
                    }

                    fromDb.TargetDate = portRequest?.TargetDate;
                    fromDb.BillingPhone = portRequest?.BillingPhone;
                    fromDb.ProviderAccountNumber = portRequest?.ProviderAccountNumber;
                    fromDb.ProviderPIN = portRequest?.ProviderPIN;
                    fromDb.LocationType = portRequest?.LocationType;
                    fromDb.BusinessContact = portRequest?.BusinessContact;
                    fromDb.BusinessName = portRequest?.BusinessName;
                    fromDb.ResidentialFirstName = portRequest?.ResidentialFirstName;
                    fromDb.ResidentialLastName = portRequest?.ResidentialLastName;
                    fromDb.Address2 = portRequest?.Address2;
                    fromDb.CallerId = portRequest?.CallerId;
                    fromDb.PartialPort = portRequest?.PartialPort ?? fromDb.PartialPort;
                    fromDb.PartialPortDescription = portRequest?.PartialPortDescription;
                    fromDb.DateUpdated = DateTime.Now;

                    await context.SaveChangesAsync();

                    portRequest = await context.PortRequests.FirstOrDefaultAsync(x => x.OrderId == portRequest!.OrderId);
                    var numbers = await context.PortedPhoneNumbers.Where(x => x.OrderId == portRequest!.OrderId).AsNoTracking().ToArrayAsync();

                    return View("PortRequestEdit", new PortRequestResult
                    {
                        Order = order,
                        PortRequest = portRequest!,
                        PhoneNumbers = numbers,
                        Message = "Successfully updated this Port Request! 🥳",
                        AlertType = "alert-success"
                    });
                }
            }
        }

        return Redirect("/Home/PortRequests");
    }

    /// <summary>
    /// Local numbers are handled by BulkVS.
    /// Local numbers are broken up into separate port requests based on 
    /// the underlying carrier so that BulkVS will accept the port request.
    /// </summary>
    /// <param name="OrderId"></param>
    /// <returns></returns>
    [Authorize]
    [HttpPost("/Home/PortRequestUnified/{orderId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnifiedPortRequestAsync(Guid? OrderId, bool ForceManual)
    {
        // ForceManual will overwrite the Zip to a value of "1". BulkVS claims this will break their automated processes and force them to review the request manually.
        List<string> responseMessages = [];

        if (OrderId is null || OrderId == Guid.Empty)
        {
            return Redirect("/Home/PortRequests");
        }

        var order = await context.Orders.FirstOrDefaultAsync(x => x.OrderId == OrderId);

        if (order is not null)
        {
            var portRequest = await context.PortRequests.FirstOrDefaultAsync(x => x.OrderId == order.OrderId);

            if (portRequest is not null)
            {
                // Prevent duplicate submissions.
                var numbers = await context.PortedPhoneNumbers
                    .Where(x => x.OrderId == order.OrderId && string.IsNullOrWhiteSpace(x.ExternalPortRequestId)).ToArrayAsync();

                if (numbers is null || numbers.Length == 0)
                {
                    numbers = await context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).ToArrayAsync();

                    return View("PortRequestEdit", new PortRequestResult
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = numbers,
                        Message = "All of the Numbers in the Port Request have already been submitted to a vendor."
                    });
                }

                // Submit the local numbers to BulkVS in a port request.
                if (numbers.Length != 0)
                {
                    try
                    {
                        // Extract the street number from the address.
                        // https://stackoverflow.com/questions/26122519/how-to-extract-address-components-from-a-string
                        Match match = PortRequstEditRegex().Match(portRequest.Address!.Trim());
                        string streetNumber = match.Groups[2].Value;

                        var lookups = new List<LrnBulkCnam>();
                        foreach (var item in numbers)
                        {
                            var spidCheck = await LrnBulkCnam.GetAsync(item.PortedDialedNumber.AsMemory(), _bulkVSAPIKey.AsMemory());
                            lookups.Add(spidCheck);
                        }

                        var checkSameSpid = lookups.Select(x => x.spid).Distinct().ToList();

                        // If there's more than one SPID for these numbers then we need to break up the list into multiple separate port requests for BulkVS.
                        if (checkSameSpid.Count > 1)
                        {
                            var portRequests = new List<PortTnRequest>();

                            foreach (var spid in checkSameSpid)
                            {
                                var localTNs = lookups.Where(x => x.spid == spid).Select(x => x.tn).ToArray();

                                var bulkVSPortRequest = new PortTnRequest
                                {
                                    ReferenceId = string.Empty,
                                    TNList = localTNs,
                                    BTN = portRequest?.BillingPhone ?? string.Empty,
                                    SubscriberType = portRequest?.LocationType ?? string.Empty,
                                    AccountNumber = portRequest?.ProviderAccountNumber ?? string.Empty,
                                    Pin = portRequest?.ProviderPIN ?? string.Empty,
                                    Name = string.IsNullOrWhiteSpace(portRequest?.BusinessName) ? $"Accelerate Networks" : $"{portRequest.BusinessName}",
                                    Contact = string.IsNullOrWhiteSpace(portRequest?.BusinessContact) ? $"{portRequest?.ResidentialFirstName} {portRequest?.ResidentialLastName}" : portRequest.BusinessContact,
                                    StreetNumber = streetNumber,
                                    StreetName = $"{portRequest?.Address[streetNumber.Length..].Trim()} {portRequest?.Address2}",
                                    City = portRequest?.City ?? string.Empty,
                                    State = portRequest?.State ?? "WA",
                                    Zip = ForceManual ? "1" : portRequest?.Zip ?? string.Empty,
                                    RDD = portRequest?.TargetDate is not null && portRequest.TargetDate.HasValue ? portRequest!.TargetDate.GetValueOrDefault().ToString("yyyy-MM-dd") : DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"),
                                    Time = portRequest?.TargetDate is not null && portRequest.TargetDate.HasValue ? portRequest!.TargetDate.GetValueOrDefault().ToString("HH:mm:ss") : "20:00:00",
                                    PortoutPin = portRequest?.ProviderPIN ?? string.Empty,
                                    TrunkGroup = "SFO",
                                    Lidb = portRequest?.CallerId ?? string.Empty,
                                    Sms = false,
                                    Mms = false,
                                    SignLoa = false,
                                    Notify = _emailOrders
                                };

                                try
                                {
                                    var bulkResponse = await bulkVSPortRequest.PutAsync(_bulkVSusername, _bulkVSpassword).ConfigureAwait(false);

                                    if (!string.IsNullOrWhiteSpace(bulkResponse.OrderId) && portRequest is not null)
                                    {
                                        portRequest.DateSubmitted = DateTime.Now;
                                        portRequest.VendorSubmittedTo = "BulkVS";
                                        portRequest.BulkVSId = string.IsNullOrWhiteSpace(portRequest?.BulkVSId) ? bulkResponse.OrderId : $"{portRequest.BulkVSId}, {bulkResponse.OrderId}";
                                        context.PortRequests.Update(portRequest ?? new());
                                        await context.SaveChangesAsync();

                                        foreach (var number in localTNs)
                                        {
                                            var updatedNumber = numbers.Where(x => $"1{x.PortedDialedNumber}" == number).FirstOrDefault();
                                            if (updatedNumber is not null)
                                            {
                                                updatedNumber.ExternalPortRequestId = bulkResponse.OrderId ?? "No Id Provided by BulkVS";
                                                updatedNumber.IngestedFrom = "BulkVS";
                                                updatedNumber.RawResponse = JsonSerializer.Serialize(bulkResponse);
                                                context.PortedPhoneNumbers.Update(updatedNumber);
                                                await context.SaveChangesAsync();
                                            }
                                        }

                                        numbers = await context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).ToArrayAsync();

                                        // Add a note to handle scenarios where the requested FOC is to soon.
                                        PortTNNote note = new([], "If the port completion date requested is unavailable please pick the next available date and set the port to complete at 8pm that day.");

                                        await note.PostAsync(bulkResponse.OrderId ?? string.Empty, _bulkVSusername, _bulkVSpassword);

                                        if (!string.IsNullOrWhiteSpace(bulkResponse.Description))
                                        {
                                            responseMessages.Add($"{bulkResponse.Description} - {bulkResponse.Code}");
                                        }
                                    }
                                    else
                                    {
                                        Log.Fatal($"[PortRequest] Failed to submit port request to BulkVS.");

                                        numbers = await context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToArrayAsync();

                                        return View("PortRequestEdit", new PortRequestResult
                                        {
                                            Order = order,
                                            Message = $"Failed to submit port request to BulkVS. {bulkResponse.Description} - {bulkResponse.Code}",
                                            PortRequest = portRequest ?? new(),
                                            PhoneNumbers = numbers
                                        });
                                    }
                                }
                                catch (FlurlHttpException ex)
                                {
                                    var response = await ex.GetResponseStringAsync();
                                    Log.Error(response);
                                    return View("PortRequestEdit", new PortRequestResult
                                    {
                                        Order = order,
                                        PortRequest = portRequest ?? new(),
                                        PhoneNumbers = numbers,
                                        Message = "Failed to submit port request to BulkVS: " + ex.Message + " " + response
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"[PortRequest] Failed to submit port request to BulkVS.");
                                    Log.Error(ex.Message);
                                    Log.Error(ex.StackTrace?.ToString() ?? "No stack trace found.");
                                }
                            }
                        }
                        else
                        {
                            // When there's just a single SPID for this port request.
                            var TNs = lookups.Select(x => x.tn).ToArray();

                            var bulkVSPortRequest = new PortTnRequest
                            {
                                ReferenceId = string.Empty,
                                TNList = TNs,
                                BTN = portRequest.BillingPhone ?? string.Empty,
                                SubscriberType = portRequest.LocationType ?? string.Empty,
                                AccountNumber = portRequest.ProviderAccountNumber ?? string.Empty,
                                Pin = portRequest.ProviderPIN ?? string.Empty,
                                Name = string.IsNullOrWhiteSpace(portRequest.BusinessName) ? $"Accelerate Networks" : $"{portRequest.BusinessName}",
                                Contact = string.IsNullOrWhiteSpace(portRequest.BusinessContact) ? $"{portRequest.ResidentialFirstName} {portRequest.ResidentialLastName}" : portRequest.BusinessContact,
                                StreetNumber = streetNumber,
                                StreetName = $"{portRequest.Address[streetNumber.Length..].Trim()} {portRequest?.Address2}",
                                City = portRequest?.City ?? string.Empty,
                                State = portRequest?.State ?? "WA",
                                Zip = ForceManual ? "1" : portRequest?.Zip ?? string.Empty,
                                RDD = portRequest?.TargetDate is not null && portRequest.TargetDate.HasValue ? portRequest!.TargetDate.GetValueOrDefault().ToString("yyyy-MM-dd") : DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"),
                                Time = portRequest?.TargetDate is not null && portRequest.TargetDate.HasValue ? portRequest!.TargetDate.GetValueOrDefault().ToString("HH:mm:ss") : "20:00:00",
                                PortoutPin = portRequest?.ProviderPIN ?? string.Empty,
                                TrunkGroup = "SFO",
                                Lidb = portRequest?.CallerId ?? string.Empty,
                                Sms = false,
                                Mms = false,
                                SignLoa = false,
                                Notify = _emailOrders
                            };

                            try
                            {

                                var bulkResponse = await bulkVSPortRequest.PutAsync(_bulkVSusername, _bulkVSpassword).ConfigureAwait(false);
                                Log.Information(JsonSerializer.Serialize(bulkResponse));

                                if (portRequest is not null && !string.IsNullOrWhiteSpace(bulkResponse.OrderId))
                                {
                                    portRequest.DateSubmitted = DateTime.Now;
                                    portRequest.VendorSubmittedTo = "BulkVS";
                                    portRequest.BulkVSId = string.IsNullOrWhiteSpace(portRequest?.BulkVSId) ? bulkResponse.OrderId : $"{portRequest.BulkVSId}, {bulkResponse.OrderId}";
                                    context.PortRequests.Update(portRequest ?? new());
                                    await context.SaveChangesAsync();

                                    foreach (var number in numbers)
                                    {
                                        number.ExternalPortRequestId = bulkResponse.OrderId;
                                        number.RawResponse = JsonSerializer.Serialize(bulkResponse);
                                        context.PortedPhoneNumbers.Update(number);
                                        await context.SaveChangesAsync();
                                    }

                                    numbers = await context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).ToArrayAsync();

                                    // Add a note to handle senarios where the requested FOC is to soon.
                                    PortTNNote note = new([], "If the port completion date requested is unavailable please pick the next available date and set the port to complete at 8pm that day.");

                                    await note.PostAsync(bulkResponse.OrderId ?? string.Empty, _bulkVSusername, _bulkVSpassword);

                                    if (!string.IsNullOrWhiteSpace(bulkResponse.Description))
                                    {
                                        responseMessages.Add($"{bulkResponse.Description} - {bulkResponse.Code}");
                                    }
                                }
                                else
                                {
                                    Log.Fatal($"[PortRequest] Failed to submit port request to BulkVS.");

                                    return View("PortRequestEdit", new PortRequestResult
                                    {
                                        Order = order,
                                        Message = $"Failed to submit port request to BulkVS. {bulkResponse.Description} - {bulkResponse.Code}",
                                        PortRequest = portRequest ?? new(),
                                        PhoneNumbers = numbers
                                    });
                                }
                            }
                            catch (FlurlHttpException ex)
                            {
                                var response = await ex.GetResponseStringAsync();
                                Log.Error(response);
                                return View("PortRequestEdit", new PortRequestResult
                                {
                                    Order = order,
                                    PortRequest = portRequest ?? new(),
                                    PhoneNumbers = numbers,
                                    Message = "Failed to submit port request to BulkVS: " + ex.Message + " " + response
                                });
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"[PortRequest] Failed to submit port request to BulkVS.");
                                Log.Error(ex.Message);
                                Log.Error(ex.StackTrace?.ToString() ?? "No stack trace found.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[PortRequest] Failed to submit port request to BulkVS.");
                        Log.Error(ex.Message);
                        Log.Error(ex.StackTrace?.ToString() ?? "No stack trace found.");

                        return View("PortRequestEdit", new PortRequestResult
                        {
                            Order = order,
                            PortRequest = portRequest,
                            PhoneNumbers = numbers,
                            Message = "Failed to submit port request to BulkVS: " + ex.Message + " " + ex.StackTrace
                        });
                    }
                }

                // Trigger the background processes.
                order.BackgroundWorkCompleted = false;
                var orderToUpdate = await context.Orders.FirstOrDefaultAsync(x => x.OrderId == order.OrderId);
                context.Entry(orderToUpdate!).CurrentValues.SetValues(order);
                await context.SaveChangesAsync();

                Log.Information($"[Port Request] Updated Order {order.OrderId} to kick off the background work.");

                return View("PortRequestEdit", new PortRequestResult
                {
                    Order = order,
                    PortRequest = portRequest ?? new(),
                    PhoneNumbers = numbers,
                    AlertType = "alert-success",
                    Message = responseMessages.Count != 0 ? string.Join(", ", [.. responseMessages]) : "🥰 Port Request was submitted to our vendors!"
                });
            }
        }

        return Redirect("/Home/PortRequests");
    }

    [GeneratedRegex(@"([^\d]*)(\d*)(.*)")]
    private static partial Regex PortRequstEditRegex();
}