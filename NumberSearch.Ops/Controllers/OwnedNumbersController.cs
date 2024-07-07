using AccelerateNetworks.Operations;

using CsvHelper;

using FirstCom;

using Flurl.Http;

using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Models;

using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.Twilio;
using NumberSearch.Ops.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class OwnedNumbersController : Controller
{
    private readonly numberSearchContext _context;
    private readonly OpsConfig _config;

    public OwnedNumbersController(numberSearchContext context, OpsConfig opsConfig)
    {
        _context = context;
        _config = opsConfig;
    }

    public class ClientRegistration
    {
        [Key]
        public Guid ClientRegistrationId { get; set; } = Guid.NewGuid();
        [DataType(DataType.PhoneNumber)]
        public string AsDialed { get; set; } = string.Empty;
        [DataType(DataType.Url)]
        public string CallbackUrl { get; set; } = string.Empty;
        [DataType(DataType.DateTime)]
        public DateTime DateRegistered { get; set; } = DateTime.Now;
        [DataType(DataType.Password)]
        public string ClientSecret { get; set; } = string.Empty;
    }

    private Task<AccessTokenResponse> GetTokenAsync()
    {
        var loginRequest = new LoginRequest()
        {
            Email = _config.MessagingUsername,
            Password = _config.MessagingPassword,
            TwoFactorCode = string.Empty,
            TwoFactorRecoveryCode = string.Empty
        };
        return $"{_config.MessagingURL}login".PostJsonAsync(loginRequest).ReceiveJson<AccessTokenResponse>();
    }

    [Authorize]
    [Route("/Home/OwnedNumbers")]
    [Route("/Home/OwnedNumbers/{dialedNumber}")]
    public async Task<IActionResult> OwnedNumbers(string dialedNumber)
    {
        string message = string.Empty;

        if (!string.IsNullOrWhiteSpace(dialedNumber))
        {
            var owned = await _context.OwnedPhoneNumbers.AsNoTracking().FirstOrDefaultAsync(x => x.DialedNumber == dialedNumber);
            if (owned is not null && owned.DialedNumber == dialedNumber)
            {
                var orderIds = new List<Guid>();
                var localPortedNumbers = await _context.PortedPhoneNumbers.Where(x => x.PortedDialedNumber == dialedNumber).ToArrayAsync();
                var localPurchasedNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.DialedNumber == dialedNumber).ToArrayAsync();
                var e911 = await _context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == dialedNumber);

                // Get the orderIds for all the related orders.
                var portedOrders = localPortedNumbers.Where(x => x.OrderId.HasValue && x.OrderId != Guid.Empty).Select(x => x.OrderId.Value).ToList();
                orderIds.AddRange(portedOrders);
                var purchasedOrders = localPurchasedNumbers.Where(x => x.OrderId != Guid.Empty).Select(x => x.OrderId).ToList();
                orderIds.AddRange(purchasedOrders);

                var relatedOrders = new List<Order>();

                foreach (var id in orderIds.Distinct())
                {
                    var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == id);
                    if (order is not null && order.OrderId == id)
                    {
                        relatedOrders.Add(order);
                    }
                }

                return View("OwnedNumberEdit", new OwnedNumberResult
                {
                    PurchasedPhoneNumbers = localPurchasedNumbers,
                    PortedPhoneNumbers = localPortedNumbers,
                    Owned = owned,
                    EmergencyInformation = e911 ?? new(),
                    RelatedOrders = relatedOrders.ToArray(),
                });
            }

        }

        // Show all orders
        var ownedNumbers = await _context.OwnedPhoneNumbers.OrderByDescending(x => x.DialedNumber).AsNoTracking().ToListAsync();
        var portedNumbers = await _context.PortedPhoneNumbers.ToArrayAsync();
        var purchasedNumbers = await _context.PurchasedPhoneNumbers.ToArrayAsync();
        var e911s = await _context.EmergencyInformation.ToArrayAsync();
        var token = await GetTokenAsync();

        var registeredNumbers = new List<ClientRegistration>();
        int page = 1;
        try
        {
            var pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                .WithOAuthBearerToken(token.AccessToken)
                .GetJsonAsync<ClientRegistration[]>();

            while (pageResult.Length is 100)
            {
                registeredNumbers.AddRange(pageResult);
                page++;
                pageResult = await $"{_config.MessagingURL}client/all?page={page}"
                    .WithOAuthBearerToken(token.AccessToken)
                    .GetJsonAsync<ClientRegistration[]>();
            }
        }
        catch (FlurlHttpException ex)
        {
            message = "❌ Failed to get client registration data from sms.callpipe.com.";
        }

        var viewOrders = new List<OwnedNumberResult>();
        foreach (var ownedNumber in ownedNumbers)
        {
            viewOrders.Add(new OwnedNumberResult
            {
                EmergencyInformation = e911s.FirstOrDefault(x => x.DialedNumber == ownedNumber.DialedNumber) ?? new(),
                Owned = ownedNumber,
                PortedPhoneNumbers = portedNumbers.Where(x => x.PortedDialedNumber == ownedNumber.DialedNumber).ToArray(),
                PurchasedPhoneNumbers = purchasedNumbers.Where(x => x.DialedNumber == ownedNumber.DialedNumber).ToArray(),
                ClientRegistration = registeredNumbers.FirstOrDefault(x => x.AsDialed == ownedNumber.DialedNumber) ?? new()
            });
        }

        return View("OwnedNumbers", new OwnedNumberResultForm { Results = viewOrders.ToArray(), Message = message });
    }

    [Authorize]
    [Route("/Home/OwnedNumbers/{dialedNumber}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OwnedNumberUpdate(OwnedNumberResult number)
    {
        if (number is null)
        {
            return Redirect("/Home/OwnedNumbers");
        }
        else
        {
            var existing = await _context.OwnedPhoneNumbers.FirstOrDefaultAsync(x => x.DialedNumber == number.Owned.DialedNumber);
            if (existing is not null)
            {
                existing.Notes = number.Owned.Notes;
                existing.OwnedBy = number.Owned.OwnedBy;
                existing.BillingClientId = number.Owned.BillingClientId;
                existing.Active = number.Owned.Active;
                await _context.SaveChangesAsync();

                var orderIds = new List<Guid>();
                var localPortedNumbers = await _context.PortedPhoneNumbers.Where(x => x.PortedDialedNumber == existing.DialedNumber).ToArrayAsync();
                var localPurchasedNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.DialedNumber == existing.DialedNumber).ToArrayAsync();
                var e911 = await _context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == existing.DialedNumber);

                // Get the orderIds for all the related orders.
                var portedOrders = localPortedNumbers.Where(x => x.OrderId.HasValue && x.OrderId != Guid.Empty).Select(x => x.OrderId.Value).ToList();
                orderIds.AddRange(portedOrders);
                var purchasedOrders = localPurchasedNumbers.Where(x => x.OrderId != Guid.Empty).Select(x => x.OrderId).ToList();
                orderIds.AddRange(purchasedOrders);

                var relatedOrders = new List<Order>();

                foreach (var id in orderIds.Distinct())
                {
                    var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == id);
                    if (order is not null && order.OrderId == id)
                    {
                        relatedOrders.Add(order);
                    }
                }

                return View("OwnedNumberEdit", new OwnedNumberResult
                {
                    Message = $"✔️ Updated {number.Owned.DialedNumber}!",
                    AlertType = "alert-success",
                    PurchasedPhoneNumbers = localPurchasedNumbers,
                    PortedPhoneNumbers = localPortedNumbers,
                    Owned = existing,
                    RelatedOrders = relatedOrders.ToArray(),
                    EmergencyInformation = e911 ?? new()
                });
            }
            else
            {
                var portedNumbers = await _context.PortedPhoneNumbers.Where(x => x.PortedDialedNumber == number.Owned.DialedNumber).ToArrayAsync();
                var purchasedNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.DialedNumber == number.Owned.DialedNumber).ToArrayAsync();
                return View("OwnedNumberEdit", new OwnedNumberResult { Message = $"❌ Failed to update {number.Owned.DialedNumber}!", AlertType = "alert-danger", PurchasedPhoneNumbers = purchasedNumbers, PortedPhoneNumbers = portedNumbers, Owned = existing });
            }
        }
    }

    [Authorize]
    [Route("/Home/OwnedNumbers/{dialedNumber}/RegisterE911")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterE911Async(string dialedNumber, string UnparsedAddress, string AddressUnitType, string AddressUnitNumber, string FirstName, string LastName, string BusinessName)
    {
        var existing = await _context.OwnedPhoneNumbers.FirstOrDefaultAsync(x => x.DialedNumber == dialedNumber);

        if (string.IsNullOrWhiteSpace(dialedNumber) && string.IsNullOrWhiteSpace(UnparsedAddress) && existing?.DialedNumber is null)
        {
            return Redirect("/Home/OwnedNumbers/");
        }
        else
        {
            var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(dialedNumber ?? string.Empty, out var phoneNumber);

            var orderIds = new List<Guid>();
            var localPortedNumbers = await _context.PortedPhoneNumbers.Where(x => x.PortedDialedNumber == existing.DialedNumber).ToArrayAsync();
            var localPurchasedNumbers = await _context.PurchasedPhoneNumbers.Where(x => x.DialedNumber == existing.DialedNumber).ToArrayAsync();

            // Get the orderIds for all the related orders.
            var portedOrders = localPortedNumbers.Where(x => x.OrderId.HasValue && x.OrderId != Guid.Empty).Select(x => x.OrderId.Value).ToList();
            orderIds.AddRange(portedOrders);
            var purchasedOrders = localPurchasedNumbers.Where(x => x.OrderId != Guid.Empty).Select(x => x.OrderId).ToList();
            orderIds.AddRange(purchasedOrders);

            var relatedOrders = new List<Order>();

            foreach (var id in orderIds.Distinct())
            {
                var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == id);
                if (order is not null && order.OrderId == id)
                {
                    relatedOrders.Add(order);
                }
            }
            // Register the number for E911 service.
            if (phoneNumber is not null && checkParse)
            {
                try
                {
                    // Format the address information
                    Log.Information($"[Checkout] Parsing address data from {UnparsedAddress}");
                    var order = new Order
                    {
                        FirstName = FirstName ?? string.Empty,
                        LastName = LastName ?? string.Empty,
                        BusinessName = BusinessName ?? string.Empty,
                        AddressUnitType = AddressUnitType ?? string.Empty,
                        AddressUnitNumber = AddressUnitNumber ?? string.Empty,
                        UnparsedAddress = UnparsedAddress ?? string.Empty
                    };

                    var addressParts = UnparsedAddress.Split(", ");
                    if (addressParts.Length == 5)
                    {
                        order.Address = addressParts[0];
                        order.City = addressParts[1];
                        order.State = addressParts[2];
                        order.Zip = addressParts[3];
                        Log.Information($"[Checkout] Address: {order.Address} City: {order.City} State: {order.State} Zip: {order.Zip}");
                    }
                    else if (addressParts.Length == 6)
                    {
                        order.Address = addressParts[0];
                        //order.UnitTypeAndNumber = addressParts[1];
                        order.City = addressParts[2];
                        order.State = addressParts[3];
                        order.Zip = addressParts[4];
                        Log.Information($"[Checkout] Address: {order.Address} City: {order.City} State: {order.State} Zip: {order.Zip}");
                    }
                    else
                    {
                        Log.Error($"[Checkout] Failed automatic address formatting.");
                    }

                    // Fill out the address2 information from its components.
                    if (!string.IsNullOrWhiteSpace(AddressUnitNumber))
                    {
                        order.Address2 = $"{AddressUnitType} {AddressUnitNumber}";
                    }

                    string[] addressChunks = order.Address?.Split(" ") ?? Array.Empty<string>();
                    string withoutUnitNumber = string.Join(" ", addressChunks[1..]);
                    var checkAddress = await E911Record.ValidateAddressAsync(addressChunks[0], withoutUnitNumber, order.Address2 ?? string.Empty,
                        order.City ?? string.Empty, order.State ?? string.Empty, order.Zip ?? string.Empty, _config.BulkVSUsername,
                        _config.BulkVSPassword);

                    if (checkAddress.Status is "GEOCODED" && !string.IsNullOrWhiteSpace(checkAddress.AddressID))
                    {
                        Log.Information(JsonSerializer.Serialize(checkAddress));

                        try
                        {
                            var response = await E911Record.PostAsync($"1{phoneNumber.DialedNumber}",
                                string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName,
                                checkAddress.AddressID, Array.Empty<string>(), _config.BulkVSUsername, _config.BulkVSPassword);

                            if (response.Status is "Success")
                            {
                                Log.Information(JsonSerializer.Serialize(response));
                                order.E911ServiceNumber = response.TN;
                                var emergencyRecord = new EmergencyInformation
                                {
                                    AddressLine1 = response.AddressLine1,
                                    AddressLine2 = response.AddressLine2,
                                    BulkVSLastModificationDate = response.LastModification,
                                    CallerName = response.CallerName,
                                    RawResponse = JsonSerializer.Serialize(response),
                                    City = response.City,
                                    DateIngested = DateTime.Now,
                                    DialedNumber = phoneNumber.DialedNumber,
                                    Sms = response.Sms.Any() ? string.Join(',', response.Sms) : string.Empty,
                                    State = response.State,
                                    EmergencyInformationId = Guid.NewGuid(),
                                    IngestedFrom = "BulkVS",
                                    ModifiedDate = DateTime.Now,
                                    Zip = response.Zip
                                };
                                // Save the record to our database
                                _context.EmergencyInformation.Add(emergencyRecord);

                                // Updated the owned number that we registered for E911 service if it exists.
                                var owned = await _context.OwnedPhoneNumbers.FirstOrDefaultAsync(x => x.DialedNumber == phoneNumber.DialedNumber);

                                if (owned is not null && owned.DialedNumber == phoneNumber.DialedNumber)
                                {
                                    owned.EmergencyInformationId = emergencyRecord.EmergencyInformationId;
                                }

                                await _context.SaveChangesAsync();

                                var e911 = await _context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == existing.DialedNumber);

                                return View("OwnedNumberEdit", new OwnedNumberResult
                                {
                                    Message = $"Successfully registered {phoneNumber.DialedNumber} with E911! 🥳",
                                    AlertType = "alert-success",
                                    PurchasedPhoneNumbers = localPurchasedNumbers,
                                    PortedPhoneNumbers = localPortedNumbers,
                                    Owned = existing,
                                    RelatedOrders = relatedOrders.ToArray(),
                                    EmergencyInformation = e911
                                });
                            }
                            else
                            {
                                var e911 = await _context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == existing.DialedNumber);

                                return View("OwnedNumberEdit", new OwnedNumberResult
                                {
                                    Message = $"Failed to register with E911! 😠 {JsonSerializer.Serialize(response)}",
                                    AlertType = "alert-danger",
                                    PurchasedPhoneNumbers = localPurchasedNumbers,
                                    PortedPhoneNumbers = localPortedNumbers,
                                    Owned = existing,
                                    RelatedOrders = relatedOrders.ToArray(),
                                    EmergencyInformation = e911
                                });
                            }
                        }
                        catch (FlurlHttpException ex)
                        {
                            var e911 = await _context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == existing.DialedNumber);

                            return View("OwnedNumberEdit", new OwnedNumberResult
                            {
                                Message = $"Failed to register with E911! 😠 {await ex.GetResponseStringAsync()}",
                                AlertType = "alert-danger",
                                PurchasedPhoneNumbers = localPurchasedNumbers,
                                PortedPhoneNumbers = localPortedNumbers,
                                Owned = existing,
                                RelatedOrders = relatedOrders.ToArray(),
                                EmergencyInformation = e911
                            });
                        }
                    }
                    else
                    {
                        var e911 = await _context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == existing.DialedNumber);

                        return View("OwnedNumberEdit", new OwnedNumberResult
                        {
                            Message = $"❌ Failed to register with E911! 😠 Address {order.Address} {order.Address2} {order.City} {order.State} {order.Zip} failed to validate for E911 Service. {JsonSerializer.Serialize(checkAddress)}",
                            AlertType = "alert-danger",
                            PurchasedPhoneNumbers = localPurchasedNumbers,
                            PortedPhoneNumbers = localPortedNumbers,
                            Owned = existing,
                            RelatedOrders = relatedOrders.ToArray(),
                            EmergencyInformation = e911
                        });
                    }
                }
                catch (FlurlHttpException ex)
                {
                    var e911 = await _context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == existing.DialedNumber);

                    return View("OwnedNumberEdit", new OwnedNumberResult
                    {
                        Message = $"❌ Failed to register with E911! 😠 {await ex.GetResponseStringAsync()}",
                        AlertType = "alert-danger",
                        PurchasedPhoneNumbers = localPurchasedNumbers,
                        PortedPhoneNumbers = localPortedNumbers,
                        Owned = existing,
                        RelatedOrders = relatedOrders.ToArray(),
                        EmergencyInformation = e911
                    });
                }
            }
            else
            {
                var e911 = await _context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == existing.DialedNumber);

                return View("OwnedNumberEdit", new OwnedNumberResult
                {
                    Message = $"❌ Failed to register with E911! 😠 The currently selected phone number {dialedNumber} is not a valid value!",
                    AlertType = "alert-danger",
                    PurchasedPhoneNumbers = localPurchasedNumbers,
                    PortedPhoneNumbers = localPortedNumbers,
                    Owned = existing,
                    RelatedOrders = relatedOrders.ToArray(),
                    EmergencyInformation = e911 ?? new()
                });
            }
        }
    }

    [Authorize]
    [Route("/OwnedNumbers/ExportToCSV")]
    public async Task<IActionResult> ExportToCSV()
    {
        var result = new OwnedNumberResult
        {
            Message = $"❓Failed to export this CSV.",
            AlertType = "alert-warning",
        };

        var ownedNumbers = await _context.OwnedPhoneNumbers.OrderByDescending(x => x.DialedNumber).AsNoTracking().ToListAsync();
        try
        {
            var filePath = Path.GetFullPath(Path.Combine("wwwroot", "csv"));
            var fileName = $"OwnedNumbers{DateTime.Now:yyyyMMdd}.csv";
            var completePath = Path.Combine(filePath, fileName);

            using var writer = new StreamWriter(completePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            await csv.WriteRecordsAsync(ownedNumbers).ConfigureAwait(false);
            var file = new FileInfo(completePath);

            if (file.Exists)
            {
                return Redirect($"../csv/{file.Name}");
            }
            else
            {
                return View("OwnedNumbers", result);
            }
        }
        catch (Exception ex)
        {
            result.Message = $"❓Failed to export this CSV. {ex.Message} {ex.StackTrace}";
            result.AlertType = "alert-danger";
        }

        return Redirect("/Home/OwnedNumbers");
    }
}