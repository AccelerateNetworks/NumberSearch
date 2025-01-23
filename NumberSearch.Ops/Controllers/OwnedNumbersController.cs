using AccelerateNetworks.Operations;

using CsvHelper;

using Flurl.Http;

using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NumberSearch.DataAccess.BulkVS;
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
public class OwnedNumbersController(numberSearchContext context, OpsConfig opsConfig) : Controller
{
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
            Email = opsConfig.MessagingUsername,
            Password = opsConfig.MessagingPassword,
            TwoFactorCode = string.Empty,
            TwoFactorRecoveryCode = string.Empty
        };
        return $"{opsConfig.MessagingURL}login".PostJsonAsync(loginRequest).ReceiveJson<AccessTokenResponse>();
    }

    [Authorize]
    [Route("/Home/OwnedNumbers")]
    [Route("/Home/OwnedNumbers/{dialedNumber}")]
    public async Task<IActionResult> OwnedNumbers(string dialedNumber)
    {
        string message = string.Empty;

        if (!string.IsNullOrWhiteSpace(dialedNumber))
        {
            var owned = await context.OwnedPhoneNumbers.AsNoTracking().FirstOrDefaultAsync(x => x.DialedNumber == dialedNumber);
            if (owned is not null && owned.DialedNumber == dialedNumber)
            {

                var e911 = await context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == dialedNumber);
                var relatedToOrders = await GetRelatedOrdersByDialedNumberAsync(owned, context);

                return View("OwnedNumberEdit", new OwnedNumberResult
                {
                    PurchasedPhoneNumbers = relatedToOrders.localPurchasedNumbers,
                    PortedPhoneNumbers = relatedToOrders.localPortedNumbers,
                    Owned = owned,
                    EmergencyInformation = e911 ?? new(),
                    RelatedOrders = [.. relatedToOrders.relatedOrders],
                });
            }
        }

        // Show all orders
        var ownedNumbers = await context.OwnedPhoneNumbers.OrderByDescending(x => x.DialedNumber).AsNoTracking().ToListAsync();
        var portedNumbers = await context.PortedPhoneNumbers.ToArrayAsync();
        var purchasedNumbers = await context.PurchasedPhoneNumbers.ToArrayAsync();
        var e911s = await context.EmergencyInformation.ToArrayAsync();
        var token = await GetTokenAsync();

        var registeredNumbers = new List<ClientRegistration>();
        int page = 1;
        try
        {
            var pageResult = await $"{opsConfig.MessagingURL}client/all?page={page}"
                .WithOAuthBearerToken(token.AccessToken)
                .GetJsonAsync<ClientRegistration[]>();

            while (pageResult.Length is 100)
            {
                registeredNumbers.AddRange(pageResult);
                page++;
                pageResult = await $"{opsConfig.MessagingURL}client/all?page={page}"
                    .WithOAuthBearerToken(token.AccessToken)
                    .GetJsonAsync<ClientRegistration[]>();
            }
        }
        catch (FlurlHttpException ex)
        {
            message = $"❌ Failed to get client registration data from sms.callpipe.com. {ex.Message}";
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

        return View("OwnedNumbers", new OwnedNumberResultForm { Results = [.. viewOrders], Message = message });
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
            var existing = await context.OwnedPhoneNumbers.FirstOrDefaultAsync(x => x.DialedNumber == number.Owned.DialedNumber);
            if (existing is not null)
            {
                existing.Notes = number.Owned.Notes;
                existing.OwnedBy = number.Owned.OwnedBy;
                existing.BillingClientId = number.Owned.BillingClientId;
                existing.Active = number.Owned.Active;
                await context.SaveChangesAsync();

                var e911 = await context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == existing.DialedNumber);
                var relatedToOwned = await GetRelatedOrdersByDialedNumberAsync(existing, context);

                return View("OwnedNumberEdit", new OwnedNumberResult
                {
                    Message = $"✔️ Updated {number.Owned.DialedNumber}!",
                    AlertType = "alert-success",
                    PurchasedPhoneNumbers = relatedToOwned.localPurchasedNumbers,
                    PortedPhoneNumbers = relatedToOwned.localPortedNumbers,
                    Owned = existing,
                    RelatedOrders = [.. relatedToOwned.relatedOrders],
                    EmergencyInformation = e911 ?? new()
                });
            }
            else
            {
                var portedNumbers = await context.PortedPhoneNumbers.Where(x => x.PortedDialedNumber == number.Owned.DialedNumber).ToArrayAsync();
                var purchasedNumbers = await context.PurchasedPhoneNumbers.Where(x => x.DialedNumber == number.Owned.DialedNumber).ToArrayAsync();
                return View("OwnedNumberEdit", new OwnedNumberResult { Message = $"❌ Failed to update {number.Owned.DialedNumber}!", AlertType = "alert-danger", PurchasedPhoneNumbers = purchasedNumbers, PortedPhoneNumbers = portedNumbers, Owned = existing ?? new() });
            }
        }
    }

    [Authorize]
    [Route("/Home/OwnedNumbers/{dialedNumber}/RegisterE911")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterE911Async(string dialedNumber, string UnparsedAddress, string AddressUnitType, string AddressUnitNumber, string CallerName)
    {
        var existing = await context.OwnedPhoneNumbers.FirstOrDefaultAsync(x => x.DialedNumber == dialedNumber);

        if (existing is null || string.IsNullOrWhiteSpace(dialedNumber) && string.IsNullOrWhiteSpace(UnparsedAddress) && string.IsNullOrWhiteSpace(existing?.DialedNumber))
        {
            return Redirect("/Home/OwnedNumbers/");
        }
        else
        {
            var checkParse = PhoneNumbersNA.PhoneNumber.TryParse(dialedNumber ?? string.Empty, out var phoneNumber);
            var relatedToOwned = await GetRelatedOrdersByDialedNumberAsync(existing, context);
            var e911 = await context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == existing.DialedNumber);

            // Declaring it here saves us from repeating it for every error path.
            var responseModel = new OwnedNumberResult
            {
                Message = $"❌ Failed to register with E911! 😠 The currently selected phone number {dialedNumber} is not a valid value!",
                AlertType = "alert-danger",
                PurchasedPhoneNumbers = relatedToOwned.localPurchasedNumbers,
                PortedPhoneNumbers = relatedToOwned.localPortedNumbers,
                Owned = existing ?? new(),
                RelatedOrders = [.. relatedToOwned.relatedOrders],
                EmergencyInformation = e911 ?? new()
            };

            // Register the number for E911 service.
            if (checkParse && !string.IsNullOrWhiteSpace(UnparsedAddress) && existing is not null)
            {
                try
                {
                    // Format the address information
                    Log.Information("[Checkout] Parsing address data from {UnparsedAddress}", UnparsedAddress);
                    var order = new Order
                    {
                        BusinessName = CallerName,
                        AddressUnitType = AddressUnitType ?? string.Empty,
                        AddressUnitNumber = AddressUnitNumber ?? string.Empty,
                        UnparsedAddress = UnparsedAddress ?? string.Empty
                    };

                    var addressParts = UnparsedAddress?.Split(", ");
                    if (addressParts is not null && addressParts.Length == 5)
                    {
                        order.Address = addressParts[0];
                        order.City = addressParts[1];
                        order.State = addressParts[2];
                        order.Zip = addressParts[3];
                        Log.Information("[Checkout] Address: {Address} City: {City} State: {State} Zip: {Zip}", order.Address, order.City, order.State, order.Zip);
                    }
                    else if (addressParts is not null && addressParts.Length == 6)
                    {
                        order.Address = addressParts[0];
                        //order.UnitTypeAndNumber = addressParts[1];
                        order.City = addressParts[2];
                        order.State = addressParts[3];
                        order.Zip = addressParts[4];
                        Log.Information("[Checkout] Address: {Address} City: {City} State: {State} Zip: {Zip}", order.Address, order.City, order.State, order.Zip);
                    }
                    else
                    {
                        Log.Error($"[Checkout] Failed automatic address formatting.");
                        responseModel.Message = $"❌ Failed to register with E911! 😠 Address {order.Address} {order.Address2} {order.City} {order.State} {order.Zip} failed to validate for E911 Service.";
                        return View("OwnedNumberEdit", responseModel);
                    }

                    // Fill out the address2 information from its components, or use NG911 per issue #488.
                    order.Address2 = !string.IsNullOrWhiteSpace(AddressUnitNumber) ? $"{AddressUnitType} {AddressUnitNumber}" : "NG911";

                    string[] addressChunks = order.Address?.Split(" ") ?? [];
                    string withoutUnitNumber = string.Join(" ", addressChunks[1..]);
                    var checkAddress = await E911Record.ValidateAddressAsync(addressChunks[0], withoutUnitNumber,
                        order.Address2 ?? string.Empty, order.City ?? string.Empty, order.State ?? string.Empty,
                        order.Zip ?? string.Empty, opsConfig.BulkVSUsername.AsMemory(), opsConfig.BulkVSPassword.AsMemory());

                    if (checkAddress.Status is "GEOCODED" && !string.IsNullOrWhiteSpace(checkAddress.AddressID))
                    {
                        Log.Information("{@checkAddress}", checkAddress);

                        try
                        {
                            var response = await E911Record.PostAsync($"1{phoneNumber.DialedNumber}", order.BusinessName,
                                checkAddress.AddressID, [], opsConfig.BulkVSUsername.AsMemory(), opsConfig.BulkVSPassword.AsMemory());

                            if (response.Status is "Success" && existing is not null)
                            {
                                Log.Information("{@response}", response);
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
                                    //Sms = response.Sms.Length != 0 ? string.Join(',', response.Sms) : string.Empty,
                                    State = response.State,
                                    EmergencyInformationId = Guid.NewGuid(),
                                    IngestedFrom = "BulkVS",
                                    ModifiedDate = DateTime.Now,
                                    Zip = response.Zip
                                };

                                // Save the record to our database
                                context.EmergencyInformation.Add(emergencyRecord);

                                // Updated the owned number that we registered for E911 service if it exists.
                                var owned = await context.OwnedPhoneNumbers.FirstOrDefaultAsync(x => x.DialedNumber == phoneNumber.DialedNumber);

                                if (owned is not null && owned.DialedNumber == phoneNumber.DialedNumber)
                                {
                                    owned.EmergencyInformationId = emergencyRecord.EmergencyInformationId;
                                }

                                await context.SaveChangesAsync();

                                // Need a fresh copy of the E911 values here, as they've been updated.
                                e911 = await context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == existing.DialedNumber);
                                responseModel.Message = $"Successfully registered {phoneNumber.DialedNumber} with E911! 🥳 {emergencyRecord.RawResponse}";
                                responseModel.AlertType = "alert-success";
                                responseModel.EmergencyInformation = e911 ?? new();
                                return View("OwnedNumberEdit", responseModel);
                            }
                            else
                            {
                                e911 = existing is not null ? await context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == existing.DialedNumber) : new();
                                responseModel.EmergencyInformation = e911 ?? new();
                                responseModel.Message = $"Failed to register with E911! 😠 {JsonSerializer.Serialize(response)}";
                                return View("OwnedNumberEdit", responseModel);
                            }
                        }
                        catch (FlurlHttpException ex)
                        {
                            e911 = await context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == existing.DialedNumber);
                            responseModel.EmergencyInformation = e911 ?? new();
                            responseModel.Message = $"Failed to register with E911! 😠 {await ex.GetResponseStringAsync()}";
                            return View("OwnedNumberEdit", responseModel);
                        }
                    }
                    else
                    {
                        e911 = await context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == existing.DialedNumber);
                        responseModel.EmergencyInformation = e911 ?? new();
                        responseModel.Message = $"❌ Failed to register with E911! 😠 Address {order.Address} {order.Address2} {order.City} {order.State} {order.Zip} failed to validate for E911 Service. {JsonSerializer.Serialize(checkAddress)}";
                        return View("OwnedNumberEdit", responseModel);
                    }
                }
                catch (FlurlHttpException ex)
                {
                    e911 = await context.EmergencyInformation.FirstOrDefaultAsync(x => x.DialedNumber == existing.DialedNumber);
                    responseModel.EmergencyInformation = e911 ?? new();
                    responseModel.Message = $"❌ Failed to register with E911! 😠 {await ex.GetResponseStringAsync()}";
                    return View("OwnedNumberEdit", responseModel);
                }
            }
            else
            {
                return View("OwnedNumberEdit", responseModel);
            }
        }
    }

    public readonly record struct RelatedToOwned(AccelerateNetworks.Operations.PortedPhoneNumber[] localPortedNumbers, AccelerateNetworks.Operations.PurchasedPhoneNumber[] localPurchasedNumbers, AccelerateNetworks.Operations.Order[] relatedOrders);

    public async Task<RelatedToOwned> GetRelatedOrdersByDialedNumberAsync(AccelerateNetworks.Operations.OwnedPhoneNumber existing, numberSearchContext context)
    {
        List<Guid> orderIds = [];
        AccelerateNetworks.Operations.PortedPhoneNumber[] localPortedNumbers = [];
        AccelerateNetworks.Operations.PurchasedPhoneNumber[] localPurchasedNumbers = [];
        if (existing is not null)
        {
            localPortedNumbers = await context.PortedPhoneNumbers.Where(x => x.PortedDialedNumber == existing.DialedNumber).ToArrayAsync();
            localPurchasedNumbers = await context.PurchasedPhoneNumbers.Where(x => x.DialedNumber == existing.DialedNumber).ToArrayAsync();

            // Get the orderIds for all the related orders.
            var portedOrders = localPortedNumbers.Where(x => x.OrderId.HasValue && x.OrderId != Guid.Empty).Select(x => x.OrderId.GetValueOrDefault()).ToList();
            orderIds.AddRange(portedOrders);
            var purchasedOrders = localPurchasedNumbers.Where(x => x.OrderId != Guid.Empty).Select(x => x.OrderId).ToList();
            orderIds.AddRange(purchasedOrders);
        }

        List<AccelerateNetworks.Operations.Order> relatedOrders = [];

        foreach (var id in orderIds.Distinct())
        {
            var order = await context.Orders.FirstOrDefaultAsync(x => x.OrderId == id);
            if (order is not null && order.OrderId == id)
            {
                relatedOrders.Add(order);
            }
        }

        return new(localPortedNumbers, localPurchasedNumbers, [.. relatedOrders]);
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

        var ownedNumbers = await context.OwnedPhoneNumbers.OrderByDescending(x => x.DialedNumber).AsNoTracking().ToListAsync();
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