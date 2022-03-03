using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess.InvoiceNinja;

using Serilog;

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers
{
    public class TaxRatesController : Controller
    {
        private readonly string _invoiceNinjaToken;

        public TaxRatesController(IConfiguration config)
        {
            _invoiceNinjaToken = config.GetConnectionString("InvoiceNinjaToken");
        }

        [Authorize]
        [HttpGet]
        [Route("/Home/TaxRates")]
        [Route("/Home/TaxRates/{taxRateId}")]
        public async Task<IActionResult> TaxRates(int? taxRateId)
        {
            if (taxRateId != null && taxRateId > 0)
            {
                var result = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);

                return View("TaxRates", new TaxRateResult
                {
                    Rates = new TaxRate
                    {
                        data = result.data.Where(x => x.id == taxRateId).ToArray()
                    }
                });
            }
            else
            {
                // Show all orders
                var result = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);

                return View("TaxRates", new TaxRateResult
                {
                    Rates = result
                }
                );
            }
        }

        [Authorize]
        [Route("/Home/TaxRates")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaxRatesCreate(TaxRateResult location)
        {
            if (location is null || string.IsNullOrWhiteSpace(location.Zip))
            {
                return Redirect("/Home/TaxRates");
            }
            else
            {
                try
                {
                    // Retry logic because this endpoint is sketchy.
                    var specificTaxRate = new NumberSearch.DataAccess.SalesTax();
                    var retryCount = 0;

                    while (specificTaxRate?.localrate == 0M)
                    {
                        try
                        {
                            specificTaxRate = await NumberSearch.DataAccess.SalesTax.GetLocalAPIAsync(location.Address, location.City, location.Zip).ConfigureAwait(false);
                        }
                        catch
                        {
                            if (retryCount > 10)
                            {
                                throw;
                            }

                            retryCount++;
                            await Task.Delay(1000);
                            // Do nothing after waiting for a bit.
                        }
                    }

                    if (specificTaxRate is not null)
                    {
                        var rateName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(specificTaxRate.rate.name.ToLowerInvariant());
                        var taxRateName = $"{rateName}, WA - {specificTaxRate.loccode}";
                        var taxRateValue = specificTaxRate.rate1 * 100M;

                        var existingTaxRates = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);
                        var billingTaxRate = existingTaxRates.data.Where(x => x.name == taxRateName).FirstOrDefault();

                        if (billingTaxRate is null)
                        {
                            billingTaxRate = new TaxRateDatum
                            {
                                name = taxRateName,
                                rate = taxRateValue
                            };

                            var checkCreate = await billingTaxRate.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);

                            var result = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);

                            return View("TaxRates", new TaxRateResult
                            {
                                Address = location.Address ?? string.Empty,
                                City = location.City ?? string.Empty,
                                Zip = location.Zip ?? string.Empty,
                                Rates = result,
                                Message = $"{taxRateName} has been created."
                            });
                        }
                        else
                        {
                            var unchanged = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);

                            return View("TaxRates", new TaxRateResult
                            {
                                Address = location.Address ?? string.Empty,
                                City = location.City ?? string.Empty,
                                Zip = location.Zip ?? string.Empty,
                                Rates = unchanged,
                                Message = $"{taxRateName} already exists."
                            });
                        }
                    }

                    return View("TaxRates", new TaxRateResult
                    {
                        Address = location.Address ?? string.Empty,
                        City = location.City ?? string.Empty,
                        Zip = location.Zip ?? string.Empty,
                        Rates = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false),
                        Message = $"Failed to create a Tax Rate for {location.Address}, {location.City}, {location.Zip}."
                    });
                }
                catch (Exception ex)
                {
                    Log.Fatal($"[Checkout] Failed to get the Sale Tax rate for {location.Address}, {location.City}, {location.Zip}.");
                    Log.Fatal(ex.Message);
                    Log.Fatal(ex.StackTrace);
                    Log.Fatal(ex.InnerException?.Message);
                    Log.Fatal(ex.InnerException?.StackTrace);

                    var result = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);

                    return View("TaxRates", new TaxRateResult
                    {
                        Address = location.Address ?? string.Empty,
                        City = location.City ?? string.Empty,
                        Zip = location.Zip ?? string.Empty,
                        Rates = result,
                        Message = $"Failed to create a Tax Rate for {location.Address}, {location.City}, {location.Zip}."
                    });
                }
            }
        }
    }
}