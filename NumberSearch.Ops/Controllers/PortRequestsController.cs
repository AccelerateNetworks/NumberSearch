using AccelerateNetworks.Operations;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.TeliMesssage;

using Serilog;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers;

public class PortRequestsController : Controller
{
    private readonly string _azureStorage;
    private readonly Guid _teleToken;
    private readonly string _bulkVSAPIKey;
    private readonly string _bulkVSusername;
    private readonly string _bulkVSpassword;
    private readonly string _emailOrders;
    private readonly numberSearchContext _context;

    public PortRequestsController(IConfiguration config, numberSearchContext context)
    {
        _azureStorage = config.GetConnectionString("AzureStorageAccount");
        _teleToken = Guid.Parse(config.GetConnectionString("TeleAPI"));
        _emailOrders = config.GetConnectionString("EmailOrders");
        _bulkVSAPIKey = config.GetConnectionString("BulkVSAPIKEY");
        _bulkVSusername = config.GetConnectionString("BulkVSUsername");
        _bulkVSpassword = config.GetConnectionString("BulkVSPassword");
        _context = context;
    }

    public async Task<PortedPhoneNumber> VerifyPortablityAsync(string number)
    {
        var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(number, out var phoneNumber);

        if (checkParse && phoneNumber is not null)
        {
            try
            {
                var portable = await LnpCheck.IsPortableAsync(phoneNumber.DialedNumber, _teleToken).ConfigureAwait(false);

                // Fail fast
                if (portable is not true)
                {
                    Log.Information($"[Portability] {phoneNumber.DialedNumber} is not Portable.");

                    return new PortedPhoneNumber
                    {
                        PortedDialedNumber = number,
                        Portable = false
                    };
                }

                // Lookup the number.
                var checkNumber = await LrnBulkCnam.GetAsync(phoneNumber.DialedNumber, _bulkVSAPIKey).ConfigureAwait(false);

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

                var numberName = await CnamBulkVs.GetAsync(phoneNumber.DialedNumber, _bulkVSAPIKey);
                checkNumber.LIDBName = string.IsNullOrWhiteSpace(numberName?.name) ? string.Empty : numberName?.name;

                Log.Information($"[Portability] {phoneNumber.DialedNumber} is Portable.");

                var portableNumber = new PortedPhoneNumber
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
                    LrnLookup = checkNumber,
                    Portable = true
                };

                return portableNumber;
            }
            catch (Exception ex)
            {
                Log.Information($"[Portability] {number} is not Portable.");
                Log.Fatal($"[Portability] {ex.Message}");
                Log.Fatal($"[Portability] {ex.InnerException}");

                return new PortedPhoneNumber
                {
                    PortedDialedNumber = number,
                    Portable = false
                };
            }
        }
        else
        {
            Log.Information($"[Portability] {number} is not Portable. Failed NPA, NXX, XXXX parsing.");

            return new PortedPhoneNumber
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
            var order = await _context.Orders.Where(x => x.OrderId == orderId).AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId);
            if (order is not null)
            {
                var portRequest = await _context.PortRequests.Where(x => x.OrderId == order.OrderId).AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId);
                if (portRequest is not null)
                {
                    var numbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();

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
        var portRequests = await _context.PortRequests.OrderByDescending(x => x.DateSubmitted).AsNoTracking().ToListAsync();

        return View("PortRequests", portRequests);
    }

    [Authorize]
    [Route("/Home/BillImage/{orderId}/")]
    public async Task<FileContentResult> DownloadAsync(string orderId)
    {
        // Create a BlobServiceClient object which will be used to create a container client
        BlobServiceClient blobServiceClient = new BlobServiceClient(_azureStorage);

        //Create a unique name for the container
        string containerName = orderId;

        // Create the container and return a container client object
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        var files = new List<BlobItem>();

        await foreach (var item in containerClient.GetBlobsAsync())
        {
            files.Add(item);
        }

        var billImage = files.FirstOrDefault();

        if (billImage is null)
        {
            //return new FileContentResult();
        }

        var blobClient = containerClient.GetBlobClient(billImage?.Name);
        var download = await blobClient.DownloadAsync();

        var fileBytes = new byte[] { };

        using (var downloadFileStream = new MemoryStream())
        {
            await download.Value.Content.CopyToAsync(downloadFileStream);

            fileBytes = downloadFileStream.ToArray();
        }


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
            var portrequest = await _context.PortRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrderId == Guid.Parse(orderId));

            if (portrequest is not null && portrequest.OrderId == Guid.Parse(orderId))
            {
                _context.PortRequests.Remove(portrequest);
                await _context.SaveChangesAsync();
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
            var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == orderId);
            if (order is not null)
            {
                var portRequest = await _context.PortRequests.FirstOrDefaultAsync(x => x.OrderId == order.OrderId);
                if (portRequest is not null)
                {
                    var numberToRemove = await _context.PortedPhoneNumbers
                                .FirstOrDefaultAsync(x => x.OrderId == order.OrderId && x.PortedDialedNumber == dialedNumber);

                    if (numberToRemove is not null)
                    {
                        _context.PortedPhoneNumbers.Remove(numberToRemove);
                        await _context.SaveChangesAsync();

                        var productOrder = await _context.ProductOrders
                                .FirstOrDefaultAsync(x => x.OrderId == order.OrderId && x.PortedPhoneNumberId == numberToRemove.PortedPhoneNumberId);

                        if (productOrder is not null)
                        {
                            _context.ProductOrders.Remove(productOrder);
                            await _context.SaveChangesAsync();
                        }
                    }

                    var numbers = await _context.PortedPhoneNumbers.AsNoTracking().ToListAsync();

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
            var order = await _context.Orders.Where(x => x.OrderId == orderId).FirstOrDefaultAsync();

            if (order is not null)
            {
                portRequest = await _context.PortRequests.Where(x => x.OrderId == order.OrderId).FirstOrDefaultAsync();
                var numbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).ToListAsync();

                var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(dialedNumber, out var phoneNumber);

                if (portRequest is not null && checkParse && phoneNumber is not null)
                {
                    var port = await VerifyPortablityAsync(phoneNumber.DialedNumber ?? string.Empty);

                    if (port is not null && port.Portable)
                    {
                        Log.Information($"[Portability] {port.PortedDialedNumber} is Portable.");

                        port.OrderId = order.OrderId;
                        port.PortRequestId = portRequest.PortRequestId;

                        _context.PortedPhoneNumbers.Add(port);
                        await _context.SaveChangesAsync();

                        numbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).ToListAsync();

                        var productOrder = new ProductOrder
                        {
                            PortedDialedNumber = port.PortedDialedNumber,
                            PortedPhoneNumberId = port.PortedPhoneNumberId,
                            Quantity = 1,
                            CreateDate = DateTime.Now,
                            OrderId = order.OrderId,
                            ProductOrderId = Guid.NewGuid()
                        };

                        _context.ProductOrders.Add(productOrder);
                        await _context.SaveChangesAsync();

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
            var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == orderId);

            if (order is not null && portRequest is not null)
            {
                var fromDb = await _context.PortRequests.FirstOrDefaultAsync(x => x.OrderId == order.OrderId);

                if (fromDb is not null)
                {
                    portRequest.PortRequestId = fromDb.PortRequestId;

                    // If the address has changed update it.
                    if (portRequest.Address != fromDb.Address)
                    {
                        // Format the address information
                        Log.Information($"[Checkout] Parsing address data from {portRequest.Address}");
                        var addressParts = portRequest.Address?.Split(", ") ?? Array.Empty<string>();
                        if (addressParts.Length > 4)
                        {
                            portRequest.Address = addressParts[0];
                            portRequest.City = addressParts[1];
                            portRequest.State = addressParts[2];
                            portRequest.Zip = addressParts[3];
                            Log.Information($"[Checkout] Address: {portRequest.Address} City: {portRequest.City} State: {portRequest.State} Zip: {portRequest.Zip}");
                        }
                        else
                        {
                            Log.Error($"[Checkout] Failed automatic address formating.");

                            portRequest = await _context.PortRequests.FirstOrDefaultAsync(x => x.OrderId == portRequest.OrderId);

                            if (portRequest is not null)
                            {
                                var numbersFailed = await _context.PortedPhoneNumbers.Where(x => x.OrderId == portRequest.OrderId).ToListAsync();

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

                    _context.PortRequests.Update(portRequest!);
                    await _context.SaveChangesAsync();

                    portRequest = await _context.PortRequests.FirstOrDefaultAsync(x => x.OrderId == portRequest!.OrderId);
                    var numbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == portRequest!.OrderId).AsNoTracking().ToListAsync();

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
    /// This porting method combines both the TeliMessage and BulkVS porting services.
    /// Tollfree numbers are handled by the TeliMessage port request.
    /// Local numbers are handled by BulkVS.
    /// Local numbers are broken up into separate port requests based on 
    /// the underlying carrier so that BulkVS will accept the port request.
    /// </summary>
    /// <param name="OrderId"></param>
    /// <returns></returns>
    [Authorize]
    [HttpPost("/Home/PortRequestUnified/{orderId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnifiedPortRequestAsync(string OrderId)
    {
        var responseMessages = new List<string>();

        if (string.IsNullOrWhiteSpace(OrderId))
        {
            return Redirect("/Home/PortRequests");
        }
        else
        {
            var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == Guid.Parse(OrderId));
            var portRequest = await _context.PortRequests.FirstOrDefaultAsync(x => x.OrderId == order.OrderId);

            // Prevent duplicate submissions.
            var numbers = await _context.PortedPhoneNumbers
                .Where(x => x.OrderId == order.OrderId && string.IsNullOrWhiteSpace(x.ExternalPortRequestId)).ToListAsync();

            if (numbers is null || !numbers.Any())
            {
                numbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).ToListAsync();

                return View("PortRequestEdit", new PortRequestResult
                {
                    Order = order,
                    PortRequest = portRequest,
                    PhoneNumbers = numbers,
                    Message = "All of the Numbers in the Port Request have already been submitted to a vendor."
                });
            }

            // Split the tollfree numbers out from the local numbers.
            var tollfreeNumbers = numbers.Where(x => PhoneNumbersNA.AreaCode.IsTollfree(x.PortedDialedNumber)).ToList();
            var localNumbers = numbers.Where(x => !PhoneNumbersNA.AreaCode.IsTollfree(x.PortedDialedNumber)).ToList();

            // Submit the tollfree numbers to TeliMessage in a port request.
            if (tollfreeNumbers.Any())
            {
                try
                {
                    var teliResponse = await LnpCreate.GetAsync(portRequest.BillingPhone, portRequest.LocationType, portRequest.BusinessContact,
                        portRequest.BusinessName, portRequest.ResidentialFirstName, portRequest.ResidentialLastName, portRequest.ProviderAccountNumber,
                        portRequest.Address, portRequest.Address2, portRequest.City, portRequest.State, portRequest.Zip, portRequest.PartialPort,
                        portRequest.PartialPortDescription, portRequest.WirelessNumber, portRequest.CallerId, portRequest.BillImagePath,
                        tollfreeNumbers.Select(x => x.PortedDialedNumber).ToArray(), _teleToken).ConfigureAwait(false);

                    if (teliResponse is not null && !string.IsNullOrWhiteSpace(teliResponse.data.id))
                    {
                        portRequest.TeliId = teliResponse.data.id;
                        portRequest.DateSubmitted = DateTime.Now;
                        portRequest.VendorSubmittedTo = "TeliMessage";
                        _context.PortRequests.Update(portRequest);
                        await _context.SaveChangesAsync();

                        foreach (var number in tollfreeNumbers)
                        {
                            number.ExternalPortRequestId = teliResponse.data.id;
                            number.RawResponse = JsonSerializer.Serialize(teliResponse);
                            _context.PortedPhoneNumbers.Update(number);
                            await _context.SaveChangesAsync();
                        }

                        numbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == portRequest.OrderId).ToListAsync();
                    }
                }
                catch (Exception ex)
                {
                    Log.Fatal($"[PortRequest] Failed to submit port request to Teli.");
                    Log.Error(ex.Message);
                    Log.Error(ex.StackTrace.ToString());

                    numbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == portRequest.OrderId).AsNoTracking().ToListAsync();

                    return View("PortRequestEdit", new PortRequestResult
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = numbers,
                        Message = "Failed to submit port request to Teli: " + ex.Message + " " + ex.StackTrace
                    });
                }
            }

            // Submit the local numbers to BulkVS in a port request.
            if (localNumbers.Any())
            {
                try
                {
                    // Extract the street number from the address.
                    // https://stackoverflow.com/questions/26122519/how-to-extract-address-components-from-a-string
                    Match match = Regex.Match(portRequest.Address.Trim(), @"([^\d]*)(\d*)(.*)");
                    string streetNumber = match.Groups[2].Value;

                    var lookups = new List<LrnBulkCnam>();
                    foreach (var item in localNumbers)
                    {
                        var spidCheck = await LrnBulkCnam.GetAsync(item.PortedDialedNumber, _bulkVSAPIKey).ConfigureAwait(false);
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
                                BTN = portRequest.BillingPhone,
                                SubscriberType = portRequest.LocationType,
                                AccountNumber = portRequest.ProviderAccountNumber,
                                Pin = portRequest.ProviderPIN,
                                Name = string.IsNullOrWhiteSpace(portRequest.BusinessName) ? $"Accelerate Networks" : $"{portRequest.BusinessName}",
                                Contact = string.IsNullOrWhiteSpace(portRequest.BusinessContact) ? $"{portRequest.ResidentialFirstName} {portRequest.ResidentialLastName}" : portRequest.BusinessContact,
                                StreetNumber = streetNumber,
                                StreetName = $"{portRequest.Address.Substring(streetNumber.Length).Trim()} {portRequest.Address2}",
                                City = portRequest.City,
                                State = "WA",
                                Zip = portRequest.Zip,
                                RDD = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"),
                                Time = "20:00:00",
                                PortoutPin = portRequest.ProviderPIN,
                                TrunkGroup = "SFO",
                                Lidb = portRequest.CallerId,
                                Sms = false,
                                Mms = false,
                                SignLoa = false,
                                Notify = _emailOrders
                            };

                            try
                            {
                                var bulkResponse = await bulkVSPortRequest.PutAsync(_bulkVSusername, _bulkVSpassword).ConfigureAwait(false);

                                if (bulkResponse is not null && !string.IsNullOrWhiteSpace(bulkResponse?.OrderId))
                                {
                                    portRequest.DateSubmitted = DateTime.Now;
                                    portRequest.VendorSubmittedTo = "BulkVS";
                                    _context.PortRequests.Update(portRequest);
                                    await _context.SaveChangesAsync();

                                    foreach (var number in localTNs)
                                    {
                                        var updatedNumber = localNumbers.Where(x => $"1{x.PortedDialedNumber}" == number).FirstOrDefault();
                                        updatedNumber.ExternalPortRequestId = bulkResponse?.OrderId;
                                        updatedNumber.RawResponse = JsonSerializer.Serialize(bulkResponse);
                                        _context.PortedPhoneNumbers.Update(updatedNumber);
                                        await _context.SaveChangesAsync();
                                    }

                                    numbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).ToListAsync();

                                    // Add a note to handle senarios where the requested FOC is to soon.
                                    var note = new PortTNNote
                                    {
                                        Note = "If the port completion date requested is unavailable please pick the next available date and set the port to complete at 8pm that day."
                                    };

                                    await note.PostAsync(bulkResponse?.OrderId, _bulkVSusername, _bulkVSpassword);

                                    if (!string.IsNullOrWhiteSpace(bulkResponse.Description))
                                    {
                                        responseMessages.Add($"{bulkResponse.Description} - {bulkResponse.Code}");
                                    }
                                }
                                else
                                {
                                    Log.Fatal($"[PortRequest] Failed to submit port request to BulkVS.");

                                    numbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).AsNoTracking().ToListAsync();

                                    return View("PortRequestEdit", new PortRequestResult
                                    {
                                        Order = order,
                                        Message = $"Failed to submit port request to BulkVS. {bulkResponse?.Description} - {bulkResponse?.Code}",
                                        PortRequest = portRequest,
                                        PhoneNumbers = numbers
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"[PortRequest] Failed to submit port request to BulkVS.");
                                Log.Error(ex.Message);
                                Log.Error(ex.StackTrace.ToString());
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
                            BTN = portRequest.BillingPhone,
                            SubscriberType = portRequest.LocationType,
                            AccountNumber = portRequest.ProviderAccountNumber,
                            Pin = portRequest.ProviderPIN,
                            Name = string.IsNullOrWhiteSpace(portRequest.BusinessName) ? $"Accelerate Networks" : $"{portRequest.BusinessName}",
                            Contact = string.IsNullOrWhiteSpace(portRequest.BusinessContact) ? $"{portRequest.ResidentialFirstName} {portRequest.ResidentialLastName}" : portRequest.BusinessContact,
                            StreetNumber = streetNumber,
                            StreetName = $"{portRequest.Address.Substring(streetNumber.Length).Trim()} {portRequest?.Address2}",
                            City = portRequest.City,
                            State = "WA",
                            Zip = portRequest.Zip,
                            RDD = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"),
                            Time = "20:00:00",
                            PortoutPin = portRequest.ProviderPIN,
                            TrunkGroup = "SFO",
                            Lidb = portRequest.CallerId,
                            Sms = false,
                            Mms = false,
                            SignLoa = false,
                            Notify = _emailOrders
                        };

                        var bulkResponse = await bulkVSPortRequest.PutAsync(_bulkVSusername, _bulkVSpassword).ConfigureAwait(false);
                        Log.Information(JsonSerializer.Serialize(bulkResponse));

                        if (bulkResponse is not null && !string.IsNullOrWhiteSpace(bulkResponse?.OrderId))
                        {
                            portRequest.DateSubmitted = DateTime.Now;
                            portRequest.VendorSubmittedTo = "BulkVS";
                            _context.PortRequests.Update(portRequest);
                            await _context.SaveChangesAsync();

                            foreach (var number in localNumbers)
                            {
                                number.ExternalPortRequestId = bulkResponse?.OrderId;
                                number.RawResponse = JsonSerializer.Serialize(bulkResponse);
                                _context.PortedPhoneNumbers.Update(number);
                                await _context.SaveChangesAsync();
                            }

                            numbers = await _context.PortedPhoneNumbers.Where(x => x.OrderId == order.OrderId).ToListAsync();

                            // Add a note to handle senarios where the requested FOC is to soon.
                            var note = new PortTNNote
                            {
                                Note = "If the port completion date requested is unavailable please pick the next available date and set the port to complete at 8pm that day."
                            };

                            await note.PostAsync(bulkResponse?.OrderId, _bulkVSusername, _bulkVSpassword);

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
                                PortRequest = portRequest,
                                PhoneNumbers = numbers
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[PortRequest] Failed to submit port request to BulkVS.");
                    Log.Error(ex.Message);
                    Log.Error(ex.StackTrace.ToString());

                    return View("PortRequestEdit", new PortRequestResult
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = numbers,
                        Message = "Failed to submit port request to BulkVS: " + ex.Message + " " + ex.StackTrace
                    });
                }
            }

            // Trigger the background processes to bring the ported numbers into Teli as offnet numbers for texting and E911 service.
            order.BackgroundWorkCompleted = false;
            var orderToUpdate = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == order.OrderId);
            _context.Entry(orderToUpdate).CurrentValues.SetValues(order);
            await _context.SaveChangesAsync();

            Log.Information($"[Port Request] Updated Order {order.OrderId} to kick off the background work.");

            return View("PortRequestEdit", new PortRequestResult
            {
                Order = order,
                PortRequest = portRequest,
                PhoneNumbers = numbers,
                AlertType = "alert-success",
                Message = responseMessages.Any() ? string.Join(", ", responseMessages.ToArray()) : "🥰 Port Request was submitted to our vendors!"
            });
        }
    }
}