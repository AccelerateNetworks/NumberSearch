using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using CsvHelper;

using FirstCom;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.Data247;
using NumberSearch.DataAccess.InvoiceNinja;
using NumberSearch.DataAccess.TeleMesssage;
using NumberSearch.Ops.Models;

using Serilog;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _postgresql;
        private readonly string _username;
        private readonly string _password;
        private readonly Guid _teleToken;
        private readonly string _bulkVSAPIKey;
        private readonly string _bulkVSAPISecret;
        private readonly string _invoiceNinjaToken;
        private readonly string _data247username;
        private readonly string _data247password;
        private readonly string _bulkVSusername;
        private readonly string _bulkVSpassword;
        private readonly string _emailOrders;
        private readonly string _azureStorage;

        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            _configuration = config;
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
            _username = config.GetConnectionString("PComNetUsername");
            _password = config.GetConnectionString("PComNetPassword");
            _teleToken = Guid.Parse(config.GetConnectionString("TeleAPI"));
            _bulkVSAPIKey = config.GetConnectionString("BulkVSAPIKEY");
            _bulkVSAPISecret = config.GetConnectionString("BulkVSAPISecret");
            _bulkVSusername = config.GetConnectionString("BulkVSUsername");
            _bulkVSpassword = config.GetConnectionString("BulkVSPassword");
            _invoiceNinjaToken = config.GetConnectionString("InvoiceNinjaToken");
            _data247username = config.GetConnectionString("Data247Username");
            _data247password = config.GetConnectionString("Data247Password");
            _emailOrders = config.GetConnectionString("EmailOrders");
            _azureStorage = config.GetConnectionString("AzureStorageAccount");
        }

        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize]
        [Route("/")]
        [Route("/Home/Order/")]
        [Route("/Home/Order/{orderId}")]
        public async Task<IActionResult> Orders(Guid? orderId)
        {
            if (orderId is null)
            {
                // Show all orders
                var orders = await Order.GetAllAsync(_postgresql).ConfigureAwait(false);
                var portRequests = await PortRequest.GetAllAsync(_postgresql).ConfigureAwait(false);
                var productOrders = await ProductOrder.GetAllAsync(_postgresql).ConfigureAwait(false);
                var purchasedNumbers = await PurchasedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);
                var verifiedNumbers = await VerifiedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);
                var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
                var services = await Service.GetAllAsync(_postgresql).ConfigureAwait(false);
                var pairs = new List<OrderProducts>();

                foreach (var order in orders)
                {
                    var orderProductOrders = productOrders.Where(x => x.OrderId == order.OrderId).ToArray();
                    var portRequest = portRequests.Where(x => x.OrderId == order.OrderId).FirstOrDefault();

                    pairs.Add(new OrderProducts
                    {
                        Order = order,
                        PortRequest = portRequest,
                        ProductOrders = orderProductOrders
                    });
                }

                return View("Orders", new OrderResult
                {
                    Orders = pairs,
                    Products = products,
                    Services = services,
                    PurchasedPhoneNumbers = purchasedNumbers,
                    VerifiedPhoneNumbers = verifiedNumbers
                });
            }
            else
            {
                var order = await Order.GetByIdAsync(orderId ?? new Guid(), _postgresql).ConfigureAwait(false);

                return View("OrderEdit", order);
            }
        }

        [Authorize]
        [Route("/Home/Order/{orderId}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderUpdate(Order? order)
        {
            if (order is null)
            {
                return Redirect("/Home/Order");
            }
            else
            {
                var existingOrder = await Order.GetByIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                order.BillingClientId = existingOrder.BillingClientId;
                order.BillingInvoiceId = existingOrder.BillingInvoiceId;
                order.BillingInvoiceReoccuringId = existingOrder.BillingInvoiceReoccuringId;
                order.DateSubmitted = existingOrder.DateSubmitted;

                var checkUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                return View("OrderEdit", order);
            }
        }

        [Authorize]
        [Route("/Home/Order/{orderId}/Delete")]
        public async Task<IActionResult> OrderDelete(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                return Redirect("/Home/Order");
            }
            else
            {
                var order = await Order.GetByIdAsync(Guid.Parse(orderId), _postgresql).ConfigureAwait(false);

                if (order is not null && order.OrderId == Guid.Parse(orderId))
                {
                    var checkDelete = await order.DeleteAsync(_postgresql).ConfigureAwait(false);
                }

                return Redirect("/Home/Order");
            }
        }

        [Authorize]
        [Route("/Home/NumberOrders")]
        [Route("/Home/NumberOrder/{orderId}")]
        [Route("/Home/NumberOrders/{dialedNumber}")]
        public async Task<IActionResult> NumberOrders(Guid? orderId, string dialedNumber)
        {
            if (orderId.HasValue)
            {
                var orders = await PurchasedPhoneNumber.GetByOrderIdAsync(orderId ?? Guid.Empty, _postgresql).ConfigureAwait(false);

                if (orders is not null && orders.Any())
                {
                    foreach (var order in orders)
                    {
                        // Update the product orders here.
                    }
                }

                return View("NumberOrders", orders.OrderByDescending(x => x.DateOrdered));
            }
            else if (string.IsNullOrWhiteSpace(dialedNumber))
            {
                // Show all orders
                var orders = await PurchasedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("NumberOrders", orders.OrderByDescending(x => x.DateOrdered));
            }
            else
            {
                var order = await PurchasedPhoneNumber.GetByDialedNumberAsync(dialedNumber, _postgresql).ConfigureAwait(false);

                return View("NumberOrders", new List<PurchasedPhoneNumber> { order });
            }
        }

        [Authorize]
        [Route("/Home/NumbersToVerify")]
        [Route("/Home/NumbersToVerify/{orderId}")]
        public async Task<IActionResult> NumbersToVerify(Guid? orderId)
        {
            if (orderId.HasValue)
            {
                var orders = await VerifiedPhoneNumber.GetByOrderIdAsync(orderId ?? Guid.Empty, _postgresql).ConfigureAwait(false);

                if (orders is not null && orders.Any())
                {
                    foreach (var order in orders)
                    {
                        // Update the product orders here.
                    }
                }

                return View("NumbersToVerify", orders);
            }
            else
            {
                // Show all orders
                var orders = await VerifiedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("NumbersToVerify", orders.OrderByDescending(x => x.DateToExpire));
            }
        }

        [Authorize]
        [Route("/Home/EmergencyInformation")]
        [Route("/Home/EmergencyInformation/{dialedNumber}")]
        public async Task<IActionResult> AllEmergencyInformation(string dialedNumber)
        {
            if (string.IsNullOrWhiteSpace(dialedNumber))
            {
                // Show all orders
                var info = await EmergencyInformation.GetAllAsync(_postgresql).ConfigureAwait(false);
                return View("EmergencyInformation", info.OrderByDescending(x => x.DateIngested));
            }
            else
            {
                var info = await EmergencyInformation.GetByDialedNumberAsync(dialedNumber, _postgresql).ConfigureAwait(false);
                return View("EmergencyInformationEdit", info.FirstOrDefault());
            }
        }

        [Authorize]
        [Route("/Home/OwnedNumbers")]
        [Route("/Home/OwnedNumbers/{dialedNumber}")]
        public async Task<IActionResult> OwnedNumbers(string dialedNumber)
        {
            if (string.IsNullOrWhiteSpace(dialedNumber))
            {
                // Show all orders
                var orders = await OwnedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);
                return View("OwnedNumbers", orders.OrderByDescending(x => x.DialedNumber));
            }
            else
            {
                var order = await OwnedPhoneNumber.GetByDialedNumberAsync(dialedNumber, _postgresql).ConfigureAwait(false);
                return View("OwnedNumberEdit", order);
            }
        }

        [Authorize]
        [Route("/Home/OwnedNumbers/{dialedNumber}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OwnedNumberUpdate(OwnedPhoneNumber number)
        {
            if (number is null)
            {
                return Redirect("/Home/OwnedNumbers");
            }
            else
            {
                var order = await OwnedPhoneNumber.GetByDialedNumberAsync(number.DialedNumber, _postgresql).ConfigureAwait(false);
                order.Notes = number.Notes;
                order.OwnedBy = number.OwnedBy;
                order.BillingClientId = number.BillingClientId;
                order.Active = number.Active;
                order.SPID = order.SPID;
                order.SPIDName = order.SPIDName;

                var checkUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                order = await OwnedPhoneNumber.GetByDialedNumberAsync(number.DialedNumber, _postgresql).ConfigureAwait(false);
                return View("OwnedNumberEdit", order);
            }
        }

        [Authorize]
        public async Task<IActionResult> ExportNumberOrders()
        {
            var orders = await PurchasedPhoneNumber.GetAllAsync(_postgresql).ConfigureAwait(false);

            var filePath = Path.GetFullPath(Path.Combine("wwwroot", "csv"));
            var fileName = $"PurchasedNumbers{DateTime.Now:yyyyMMdd}.csv";
            var completePath = Path.Combine(filePath, fileName);

            using var writer = new StreamWriter(completePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            await csv.WriteRecordsAsync(orders).ConfigureAwait(false);
            var file = new FileInfo(completePath);

            if (file.Exists)
            {
                return Redirect($"../csv/{file.Name}");
            }
            else
            {
                return View("NumberOrders", orders.OrderByDescending(x => x.DateOrdered));
            }
        }

        [Authorize]
        [Route("/Home/PortRequests")]
        [Route("/Home/PortRequests/{orderId}")]
        public async Task<IActionResult> PortRequests(Guid? orderId)
        {
            if (orderId is not null && orderId.HasValue)
            {
                var order = await Order.GetByIdAsync(orderId ?? Guid.Empty, _postgresql).ConfigureAwait(false);
                var portRequest = await PortRequest.GetByOrderIdAsync(orderId ?? Guid.Empty, _postgresql).ConfigureAwait(false);
                var numbers = await PortedPhoneNumber.GetByOrderIdAsync(orderId ?? Guid.Empty, _postgresql).ConfigureAwait(false);

                return View("PortRequestEdit", new PortRequestResult
                {
                    Order = order,
                    PortRequest = portRequest,
                    PhoneNumbers = numbers
                });
            }
            else
            {
                // Show all orders
                var portRequests = await PortRequest.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("PortRequests", portRequests.OrderByDescending(x => x.DateSubmitted));
            }
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

            var blobClient = containerClient.GetBlobClient(billImage.Name);
            var download = await blobClient.DownloadAsync();

            var fileBytes = new byte[] { };

            using (var downloadFileStream = new MemoryStream())
            {
                await download.Value.Content.CopyToAsync(downloadFileStream);

                fileBytes = downloadFileStream.ToArray();
            }


            return new FileContentResult(fileBytes, download.Value.ContentType)
            {
                FileDownloadName = billImage.Name
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
                var portrequest = await PortRequest.GetByOrderIdAsync(Guid.Parse(orderId), _postgresql).ConfigureAwait(false);

                if (portrequest is not null && portrequest.OrderId == Guid.Parse(orderId))
                {
                    var checkDelete = await portrequest.DeleteAsync(_postgresql).ConfigureAwait(false);
                }

                return Redirect("/Home/PortRequests");
            }
        }

        [Authorize]
        [HttpGet("/Home/Shipment/")]
        [HttpGet("/Home/Shipment/{ProductShipmentId}")]
        public async Task<IActionResult> ShipmentsAsync(Guid? ProductShipmentId)
        {
            if (ProductShipmentId is null || !ProductShipmentId.HasValue)
            {
                var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
                var shipments = await ProductShipment.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("Shipments", new InventoryResult { Products = products, ProductShipments = shipments });
            }
            else
            {
                var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
                var checkExists = await ProductShipment.GetByIdAsync(ProductShipmentId ?? new Guid(), _postgresql).ConfigureAwait(false);

                return View("Shipments", new InventoryResult { Products = products, ProductShipments = new List<ProductShipment> { checkExists }, Shipment = checkExists });
            }
        }

        [Authorize]
        [Route("/Home/Shipment")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShipmentCreate(ProductShipment shipment)
        {
            if (shipment.ProductId == Guid.Empty)
            {
                return Redirect("/Home/Shipments");
            }
            else
            {
                shipment.DateCreated = DateTime.Now;
                var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
                var checkExists = await ProductShipment.GetByIdAsync(shipment.ProductShipmentId, _postgresql).ConfigureAwait(false);

                if (checkExists is null)
                {
                    if (string.IsNullOrWhiteSpace(shipment.Name))
                    {
                        shipment.Name = products.Where(x => x.ProductId == shipment.ProductId).FirstOrDefault().Name;
                    }
                    var checkSave = await shipment.PostAsync(_postgresql).ConfigureAwait(false);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(shipment.Name))
                    {
                        shipment.Name = products.Where(x => x.ProductId == shipment.ProductId).FirstOrDefault().Name;
                    }

                    var checkUpdate = await shipment.PutAsync(_postgresql).ConfigureAwait(false);
                }

                var shipments = await ProductShipment.GetAllAsync(_postgresql).ConfigureAwait(false);

                // Update all product inventory counts when a shipment is added or updated.
                foreach (var product in products)
                {
                    var relatedShipments = shipments.Where(x => x.ProductId == product.ProductId);
                    var instockItems = relatedShipments.Where(x => x.ShipmentType == "Instock").Sum(x => x.Quantity);
                    var assignedItems = relatedShipments.Where(x => x.ShipmentType == "Assigned").Sum(x => x.Quantity);
                    product.QuantityAvailable = instockItems - assignedItems;

                    var checkStock = await product.PutAsync(_postgresql).ConfigureAwait(false);
                }

                return View("Shipments", new InventoryResult { Products = products, ProductShipments = shipments });
            }
        }

        [Authorize]
        [Route("/Home/Shipment/{productShipmentId}/Delete")]
        public async Task<IActionResult> ProductShipmentDelete(Guid productShipmentId)
        {
            if (productShipmentId == Guid.Empty)
            {
                return Redirect("/Home/Shipment");
            }
            else
            {
                var order = await ProductShipment.GetByIdAsync(productShipmentId, _postgresql).ConfigureAwait(false);

                if (order is not null && order.ProductShipmentId == productShipmentId)
                {
                    var checkDelete = await order.DeleteAsync(_postgresql).ConfigureAwait(false);
                }

                return Redirect("/Home/Shipment");
            }
        }


        [Authorize]
        [HttpGet("/Home/Product/")]
        [HttpGet("/Home/Product/{ProductId}")]
        public async Task<IActionResult> ProductsAsync(Guid? ProductId)
        {
            if (ProductId is null || !ProductId.HasValue)
            {
                var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("Products", new InventoryResult { Products = products });
            }
            else
            {
                var products = await Product.GetByIdAsync(ProductId ?? new Guid(), _postgresql).ConfigureAwait(false);

                return View("Products", new InventoryResult { Products = new List<Product> { products }, Product = products });
            }
        }


        [Authorize]
        [Route("/Home/Product")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProductCreate(Product product)
        {
            var checkExists = await Product.GetByIdAsync(product.ProductId, _postgresql).ConfigureAwait(false);

            if (checkExists is null)
            {
                var checkSave = await product.PostAsync(_postgresql).ConfigureAwait(false);
            }
            else
            {
                var checkUpdate = await product.PutAsync(_postgresql).ConfigureAwait(false);
            }

            var products = await Product.GetAllAsync(_postgresql).ConfigureAwait(false);
            var shipments = await ProductShipment.GetAllAsync(_postgresql).ConfigureAwait(false);

            return View("Products", new InventoryResult { Products = products, ProductShipments = shipments });
        }

        [Authorize]
        [Route("/Home/Product/{productId}/Delete")]
        public async Task<IActionResult> ProductDelete(Guid productId)
        {
            if (productId == Guid.Empty)
            {
                return Redirect("/Home/Product");
            }
            else
            {
                var order = await Product.GetByIdAsync(productId, _postgresql).ConfigureAwait(false);

                if (order is not null && order.ProductId == productId)
                {
                    var checkDelete = await order.DeleteAsync(_postgresql).ConfigureAwait(false);
                }

                return Redirect("/Home/Product");
            }
        }

        [Authorize]
        [HttpGet]
        [Route("/Home/Coupons")]
        [Route("/Home/Coupons/{couponId}")]
        public async Task<IActionResult> Coupons(Guid? couponId)
        {
            if (couponId is null)
            {
                var results = await Coupon.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("Coupons", new CouponResult { Coupons = results });
            }
            else
            {
                // Show all orders
                var result = await Coupon.GetByIdAsync(couponId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);

                return View("Coupons", new CouponResult { Coupons = new List<Coupon> { result } });
            }
        }


        [Authorize]
        [Route("/Home/Coupon")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CouponCreate(Coupon coupon)
        {
            var checkExists = await Coupon.GetByIdAsync(coupon.CouponId, _postgresql).ConfigureAwait(false);

            if (checkExists is null)
            {
                var checkSave = await coupon.PostAsync(_postgresql).ConfigureAwait(false);
            }
            else
            {
                var checkUpdate = await coupon.PutAsync(_postgresql).ConfigureAwait(false);
            }

            var coupons = await Coupon.GetAllAsync(_postgresql).ConfigureAwait(false);

            return View("Coupons", new CouponResult { Coupons = coupons });
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
                    var specificTaxRate = new SalesTax();
                    var retryCount = 0;

                    while (specificTaxRate?.localrate == 0M)
                    {
                        try
                        {
                            specificTaxRate = await SalesTax.GetLocalAPIAsync(location.Address, location.City, location.Zip).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            if (retryCount > 10)
                            {
                                throw ex;
                            }

                            retryCount++;
                            await Task.Delay(1000);
                            // Do nothing after waiting for a bit.
                        }
                    }

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
                catch (Exception ex)
                {
                    Log.Fatal($"[Checkout] Failed to get the Sale Tax rate for {location.Address}, {location.City}, {location.Zip}.");
                    Log.Fatal(ex.Message);
                    Log.Fatal(ex.StackTrace);
                    Log.Fatal(ex.InnerException.Message);
                    Log.Fatal(ex.InnerException.StackTrace);

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

        [Authorize]
        [Route("/Home/PortRequests/{orderId}/{dialedNumber}")]
        public async Task<IActionResult> RemovePortedPhoneNumber(Guid? orderId, string dialedNumber)
        {
            if (!string.IsNullOrWhiteSpace(dialedNumber))
            {
                var order = await Order.GetByIdAsync(orderId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);
                var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                var numberToRemove = numbers.Where(x => x.PortedDialedNumber == dialedNumber).FirstOrDefault();

                if (numberToRemove is not null)
                {
                    var checkDelete = await numberToRemove.DeleteAsync(_postgresql).ConfigureAwait(false);

                    if (checkDelete)
                    {
                        numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                    }
                }

                return View("PortRequestEdit", new PortRequestResult
                {
                    Order = order,
                    PortRequest = portRequest,
                    PhoneNumbers = numbers
                });
            }
            else
            {
                return Redirect("/Home/PortRequests");
            }
        }

        [Authorize]
        [HttpPost("/Home/PortRequests/{orderId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PortRequestUpdate(PortRequestResult result, Guid? orderId, string dialedNumber)
        {
            var portRequest = result?.PortRequest ?? null;

            if (!string.IsNullOrWhiteSpace(dialedNumber))
            {
                var order = await Order.GetByIdAsync(orderId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);
                portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                var Query = dialedNumber;
                // Clean up the query.
                Query = Query?.Trim();

                // Parse the query.
                var converted = new List<char>();
                foreach (var letter in Query)
                {
                    // Allow digits.
                    if (char.IsDigit(letter))
                    {
                        converted.Add(letter);
                    }
                    // Allow stars.
                    else if (letter == '*')
                    {
                        converted.Add(letter);
                    }
                    // Convert letters to digits.
                    // This is disabled so as to avoid taking a dependancy on the Mvc project.
                    //else if (char.IsLetter(letter))
                    //{
                    //    converted.Add(SearchController.LetterToKeypadDigit(letter));
                    //}
                    // Drop everything else.
                }

                // Drop leading 1's to improve the copy/paste experiance.
                if (converted[0] == '1' && converted.Count >= 10)
                {
                    converted.Remove('1');
                }

                Query = new string(converted.ToArray());

                if (Query != null && Query?.Length == 10)
                {
                    var dialedPhoneNumber = Query;

                    bool checkNpa = int.TryParse(dialedPhoneNumber.Substring(0, 3), out int npa);
                    bool checkNxx = int.TryParse(dialedPhoneNumber.Substring(3, 3), out int nxx);
                    bool checkXxxx = int.TryParse(dialedPhoneNumber.Substring(6, 4), out int xxxx);

                    if (checkNpa && checkNxx && checkXxxx)
                    {
                        try
                        {
                            var portable = await LnpCheck.IsPortable(dialedPhoneNumber, _teleToken).ConfigureAwait(false);

                            // Lookup the number.
                            var checkNumber = await LrnBulkCnam.GetAsync(dialedPhoneNumber, _bulkVSAPIKey).ConfigureAwait(false);

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

                            var numberName = new RateCenterLookup();
                            try
                            {
                                numberName = await RateCenterLookup.GetAsync(dialedPhoneNumber, _data247username, _data247password).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"[Lookups] Failed to get LIBDName from Data 24/7 for number {dialedPhoneNumber}.");
                                Log.Error(ex.Message);
                                Log.Error(ex.InnerException.ToString());
                            }

                            checkNumber.LIDBName = string.IsNullOrWhiteSpace(numberName?.response?.results?.FirstOrDefault()?.name) ? string.Empty : numberName?.response?.results?.FirstOrDefault()?.name;

                            if (portable)
                            {
                                Log.Information($"[Portability] {dialedPhoneNumber} is Portable.");

                                var port = new PortedPhoneNumber
                                {
                                    PortedDialedNumber = dialedPhoneNumber,
                                    NPA = npa,
                                    NXX = nxx,
                                    XXXX = xxxx,
                                    City = "Unknown City",
                                    State = "Unknown State",
                                    DateIngested = DateTime.Now,
                                    IngestedFrom = "UserInput",
                                    Wireless = wireless,
                                    LrnLookup = checkNumber,
                                    OrderId = order.OrderId,
                                    PortRequestId = portRequest.PortRequestId,
                                };

                                var checkSave = await port.PostAsync(_postgresql).ConfigureAwait(false);

                                if (checkSave)
                                {
                                    numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                var port = new PortedPhoneNumber
                                {
                                    PortedDialedNumber = dialedPhoneNumber,
                                    NPA = npa,
                                    NXX = nxx,
                                    XXXX = xxxx,
                                    City = "Unknown City",
                                    State = "Unknown State",
                                    DateIngested = DateTime.Now,
                                    IngestedFrom = "UserInput",
                                    Wireless = wireless,
                                    LrnLookup = checkNumber,
                                    OrderId = order.OrderId,
                                    PortRequestId = portRequest.PortRequestId,
                                };
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Fatal($"[Portability] {ex.Message}");

                            var port = new PortedPhoneNumber
                            {
                                PortedDialedNumber = dialedPhoneNumber,
                                NPA = npa,
                                NXX = nxx,
                                XXXX = xxxx,
                                City = "Unknown City",
                                State = "Unknown State",
                                DateIngested = DateTime.Now,
                                IngestedFrom = "UserInput"
                            };
                        }
                    }
                }

                return View("PortRequestEdit", new PortRequestResult
                {
                    Order = order,
                    PortRequest = portRequest,
                    PhoneNumbers = numbers
                });
            }
            else if (portRequest is null)
            {
                return Redirect("/Home/PortRequests");
            }
            else
            {
                var order = await Order.GetByIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);
                var fromDb = await PortRequest.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);

                portRequest.PortRequestId = fromDb.PortRequestId;

                var checkUpdate = portRequest.PutAsync(_postgresql).ConfigureAwait(false);

                portRequest = await PortRequest.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);
                var numbers = await PortedPhoneNumber.GetByOrderIdAsync(portRequest.OrderId, _postgresql).ConfigureAwait(false);

                return View("PortRequestEdit", new PortRequestResult
                {
                    Order = order,
                    PortRequest = portRequest,
                    PhoneNumbers = numbers
                });
            }
        }

        [Authorize]
        [HttpPost("/Home/PortRequestsTeli/{orderId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PortRequestSendToTeli(string OrderId)
        {
            if (string.IsNullOrWhiteSpace(OrderId))
            {
                return Redirect("/Home/PortRequests");
            }
            else
            {
                var order = await Order.GetByIdAsync(Guid.Parse(OrderId), _postgresql).ConfigureAwait(false);
                var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                numbers = numbers.Where(x => string.IsNullOrWhiteSpace(x.ExternalPortRequestId)).ToList();

                // Prevent duplicate submissions.
                if (numbers is null || !numbers.Any())
                {
                    numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                    return View("PortRequestEdit", new PortRequestResult
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = numbers,
                        Message = "All of the Numbers in the Port Request have already been submitted to a vendor."
                    });
                }

                try
                {
                    var teliResponse = await LnpCreate.GetAsync(portRequest, numbers, _teleToken).ConfigureAwait(false);
                    portRequest.TeliId = teliResponse.data.id;
                    portRequest.DateSubmitted = DateTime.Now;
                    portRequest.VendorSubmittedTo = "TeliMessage";
                    var checkUpdate = portRequest.PutAsync(_postgresql).ConfigureAwait(false);

                    foreach (var number in numbers)
                    {
                        number.ExternalPortRequestId = teliResponse.data.id;
                        var checkUpdateId = await number.PutAsync(_postgresql).ConfigureAwait(false);
                    }

                    numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                    return View("PortRequestEdit", new PortRequestResult
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = numbers
                    });
                }
                catch (Exception ex)
                {
                    Log.Fatal($"[PortRequest] Failed to submit port request to Teli.");
                    Log.Error(ex.Message);
                    Log.Error(ex.StackTrace.ToString());

                    numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                    return View("PortRequestEdit", new PortRequestResult
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = numbers,
                        Message = "Failed to submit port request to Teli: " + ex.Message + " " + ex.StackTrace
                    });
                }
            }
        }

        [Authorize]
        [HttpPost("/Home/PortRequestsBulkVS/{orderId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PortRequestSendToBulkVS(string OrderId)
        {
            if (string.IsNullOrWhiteSpace(OrderId))
            {
                return Redirect("/Home/PortRequests");
            }
            else
            {
                var order = await Order.GetByIdAsync(Guid.Parse(OrderId), _postgresql).ConfigureAwait(false);
                var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                numbers = numbers.Where(x => string.IsNullOrWhiteSpace(x.ExternalPortRequestId)).ToList();

                // Prevent duplicate submissions.
                if (numbers is null || !numbers.Any())
                {
                    numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                    return View("PortRequestEdit", new PortRequestResult
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = numbers,
                        Message = "All of the Numbers in the Port Request have already been submitted to a vendor."
                    });
                }

                try
                {
                    // Extract the street number from the address.
                    // https://stackoverflow.com/questions/26122519/how-to-extract-address-components-from-a-string
                    Match match = Regex.Match(portRequest.Address.Trim(), @"([^\d]*)(\d*)(.*)");
                    string streetNumber = match.Groups[2].Value;

                    var lookups = new List<LrnBulkCnam>();
                    foreach (var item in numbers)
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
                                Sms = true,
                                Mms = true,
                                SignLoa = false,
                                Notify = _emailOrders
                            };

                            try
                            {
                                var bulkResponse = await bulkVSPortRequest.PutAsync(_bulkVSusername, _bulkVSpassword).ConfigureAwait(false);

                                if (!string.IsNullOrWhiteSpace(bulkResponse?.OrderId))
                                {
                                    // Rename this to VendorOrderId, rather than TeliId.
                                    portRequest.TeliId = bulkResponse?.OrderId;
                                    portRequest.DateSubmitted = DateTime.Now;
                                    var checkUpdate = portRequest.PutAsync(_postgresql).ConfigureAwait(false);

                                    foreach (var number in localTNs)
                                    {
                                        var updatedNumber = numbers.Where(x => $"1{x.PortedDialedNumber}" == number).FirstOrDefault();
                                        updatedNumber.ExternalPortRequestId = bulkResponse?.OrderId;
                                        var checkUpdateId = await updatedNumber.PutAsync(_postgresql).ConfigureAwait(false);
                                    }

                                    numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                    return View("PortRequestEdit", new PortRequestResult
                                    {
                                        Order = order,
                                        Message = $"{bulkResponse.Description} - {bulkResponse.Code}",
                                        PortRequest = portRequest,
                                        PhoneNumbers = numbers
                                    });
                                }
                                else
                                {
                                    Log.Fatal($"[PortRequest] Failed to submit port request to BulkVS.");

                                    numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                                    return View("PortRequestEdit", new PortRequestResult
                                    {
                                        Order = order,
                                        Message = $"{bulkResponse.Description} - {bulkResponse.Code}",
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
                            StreetName = $"{portRequest.Address.Substring(streetNumber.Length).Trim()} {portRequest.Address2}",
                            City = portRequest.City,
                            State = "WA",
                            Zip = portRequest.Zip,
                            RDD = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd"),
                            Time = "20:00:00",
                            PortoutPin = portRequest.ProviderPIN,
                            TrunkGroup = "SFO",
                            Lidb = portRequest.CallerId,
                            Sms = true,
                            Mms = true,
                            SignLoa = false,
                            Notify = _emailOrders
                        };

                        var bulkResponse = await bulkVSPortRequest.PutAsync(_bulkVSusername, _bulkVSpassword).ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(bulkResponse?.OrderId))
                        {
                            // Rename this to VendorOrderId, rather than TeliId.
                            portRequest.TeliId = bulkResponse?.OrderId;
                            portRequest.DateSubmitted = DateTime.Now;
                            portRequest.VendorSubmittedTo = "BulkVS";
                            var checkUpdate = portRequest.PutAsync(_postgresql).ConfigureAwait(false);

                            foreach (var number in numbers)
                            {
                                number.ExternalPortRequestId = bulkResponse?.OrderId;
                                var checkUpdateId = await number.PutAsync(_postgresql).ConfigureAwait(false);
                            }

                            numbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                            return View("PortRequestEdit", new PortRequestResult
                            {
                                Order = order,
                                Message = $"{bulkResponse.Description} - {bulkResponse.Code}",
                                PortRequest = portRequest,
                                PhoneNumbers = numbers
                            });
                        }
                        else
                        {
                            Log.Fatal($"[PortRequest] Failed to submit port request to BulkVS.");

                            return View("PortRequestEdit", new PortRequestResult
                            {
                                Order = order,
                                Message = $"{bulkResponse.Description} - {bulkResponse.Code}",
                                PortRequest = portRequest,
                                PhoneNumbers = numbers
                            });
                        }
                    }

                    return View("PortRequestEdit", new PortRequestResult
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = numbers
                    });
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
        }

        [Authorize]
        public async Task<IActionResult> Tests(string testName, string npa, string nxx, string dialedNumber)
        {
            if (testName == "DIDInventorySearchAsync" && (!string.IsNullOrWhiteSpace(npa) || !string.IsNullOrWhiteSpace(nxx) || !string.IsNullOrWhiteSpace(dialedNumber)))
            {
                npa ??= string.Empty;
                nxx ??= string.Empty;
                dialedNumber ??= string.Empty;

                var results = await NpaNxxFirstPointCom.GetAsync(npa, nxx, dialedNumber, _username, _password).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    NPA = npa,
                    NXX = nxx,
                    DialedNumber = dialedNumber,
                    PhoneNumbersTM = results
                });
            }

            if (testName == "DIDOrderAsync" && (!string.IsNullOrWhiteSpace(dialedNumber)))
            {
                var results = await FirstPointComOrderPhoneNumber.PostAsync(dialedNumber, _username, _password).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    DialedNumber = dialedNumber,
                    PhoneNumberOrder = results
                });
            }

            if (testName == "LRNLookup" && (!string.IsNullOrWhiteSpace(dialedNumber)))
            {
                var checkNumber = await LrnLookup.GetAsync(dialedNumber, _teleToken).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    DialedNumber = dialedNumber,
                    LRNLookup = checkNumber
                });
            }

            if (testName == "didslist" && (!string.IsNullOrWhiteSpace(dialedNumber)))
            {
                var checkNumber = await DidsList.GetAsync(dialedNumber, _teleToken).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    DialedNumber = dialedNumber,
                    PhoneNumbersTM = checkNumber
                });
            }

            if (testName == "lnpcheck" && (!string.IsNullOrWhiteSpace(dialedNumber)))
            {
                var checkNumber = await LnpCheck.GetRawAsync(dialedNumber, _teleToken).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    PortabilityResponse = checkNumber
                });
            }

            if (testName == "DnSearchNpaNxx" && (!string.IsNullOrWhiteSpace(npa) || !string.IsNullOrWhiteSpace(nxx)))
            {
                npa ??= string.Empty;
                nxx ??= string.Empty;
                var checkNPA = int.TryParse(npa, out var NPA);
                var checkNXX = int.TryParse(nxx, out var NXX);

                var checkNumber = await OrderTn.GetAsync(NPA, NXX, _bulkVSusername, _bulkVSpassword).ConfigureAwait(false);

                return View("Tests", new TestResults
                {
                    PhoneNumbersBVS = checkNumber
                });
            }

            return View("Tests");
        }

        [Authorize]
        [Route("/Home/Emails")]
        [Route("/Home/Emails/{orderId}")]
        public async Task<IActionResult> Emails(Guid? orderId)
        {
            if (orderId != null && orderId.HasValue)
            {
                var emails = await Email.GetByOrderAsync(orderId ?? Guid.NewGuid(), _postgresql).ConfigureAwait(false);

                return View("Emails", emails);
            }
            else
            {
                var emails = await Email.GetAllAsync(_postgresql).ConfigureAwait(false);

                return View("Emails", emails);
            }
        }

        [Authorize]
        [HttpGet("/Home/Emails/{orderId}/Resend")]
        public async Task<IActionResult> ResendEmails(Guid orderId)
        {
            var order = await Order.GetByIdAsync(orderId, _postgresql).ConfigureAwait(false);
            order.BackgroundWorkCompleted = false;
            var checkUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

            var emails = await Email.GetAllAsync(_postgresql).ConfigureAwait(false);

            return View("Emails", emails);
        }

        [Authorize]
        public async Task<IActionResult> Ingests(int cycle, string ingestedFrom, string enabled, string runNow)
        {
            var ingests = await IngestCycle.GetAllAsync(_postgresql).ConfigureAwait(false);

            if (cycle > 0 && cycle < 24 && !string.IsNullOrWhiteSpace(ingestedFrom) && (enabled == "Enabled" || enabled == "Disabled"))
            {
                var update = ingests.Where(x => x.IngestedFrom == ingestedFrom).FirstOrDefault();

                if (update != null)
                {
                    update.CycleTime = DateTime.Now.AddHours(cycle) - DateTime.Now;
                    update.Enabled = enabled == "Enabled";
                    update.RunNow = runNow == "true";
                    update.LastUpdate = DateTime.Now;

                    var checkUpdate = await update.PutAsync(_postgresql).ConfigureAwait(false);

                    ingests = await IngestCycle.GetAllAsync(_postgresql).ConfigureAwait(false);
                }
                else
                {
                    update = new IngestCycle
                    {
                        CycleTime = DateTime.Now.AddHours(cycle) - DateTime.Now,
                        IngestedFrom = ingestedFrom,
                        Enabled = enabled == "Enabled",
                        RunNow = runNow == "true",
                        LastUpdate = DateTime.Now
                    };

                    var checkCreate = await update.PostAsync(_postgresql).ConfigureAwait(false);

                    ingests = await IngestCycle.GetAllAsync(_postgresql).ConfigureAwait(false);
                }
            }

            return View("IngestConfiguration", ingests);
        }

        /// <summary>
        /// This is the default route in this app. It's a search page that allows you to query the TeleAPI for phone numbers.
        /// </summary>
        /// <param name="query"> A complete or partial phone number. </param>
        /// <returns> A view of nothing, or the result of the query. </returns>
        [Authorize]
        [Route("/Numbers/{Query}")]
        [Route("/Numbers/")]
        public async Task<IActionResult> Numbers(string query, int page = 1)
        {
            // Fail fast
            if (string.IsNullOrWhiteSpace(query))
            {
                return View("Numbers");
            }

            // Clean up the query.
            query = query?.Trim();

            // Parse the query.
            var converted = new List<char>();
            foreach (var letter in query)
            {
                // Allow digits.
                if (char.IsDigit(letter))
                {
                    converted.Add(letter);
                }
                // Allow stars.
                else if (letter == '*')
                {
                    converted.Add(letter);
                }
                // Convert letters to digits.
                else if (char.IsLetter(letter))
                {
                    converted.Add(LetterToKeypadDigit(letter));
                }
                // Drop everything else.
            }

            // Drop leading 1's to improve the copy/paste experiance.
            if (converted[0] == '1' && converted.Count >= 10)
            {
                converted.Remove('1');
            }

            var results = await PhoneNumber.SequentialPaginatedSearchAsync(new string(converted.ToArray()), page, _postgresql).ConfigureAwait(false);
            var count = await PhoneNumber.NumberOfResultsInQuery(new string(converted.ToArray()), _postgresql).ConfigureAwait(false);

            return View("Numbers", new SearchResults
            {
                CleanQuery = new string(converted.ToArray()),
                NumberOfResults = count,
                Page = page,
                PhoneNumbers = results.ToArray(),
                Query = query
            });
        }

        public static char LetterToKeypadDigit(char letter)
        {
            // Map the chars to their keypad numerical values.
            return letter switch
            {
                '+' => '0',
                'a' or 'b' or 'c' => '2',
                'd' or 'e' or 'f' => '3',
                'g' or 'h' or 'i' => '4',
                'j' or 'k' or 'l' => '5',
                'm' or 'n' or 'o' => '6',
                'p' or 'q' or 'r' or 's' => '7',
                't' or 'u' or 'v' => '8',
                'w' or 'x' or 'y' or 'z' => '9',
                _ => '*',// The digit 1 isn't mapped to any chars on a phone keypad.
                         // If the char isn't mapped to anything, respect it's existence by mapping it to a wildcard.
            };
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
