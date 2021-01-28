using FirstCom;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;
using NumberSearch.DataAccess.Data247;
using NumberSearch.DataAccess.InvoiceNinja;
using NumberSearch.DataAccess.TeleMesssage;

using Serilog;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class CartController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly Guid _teleToken;
        private readonly string _postgresql;
        private readonly int _CallFlow;
        private readonly int _ChannelGroup;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _fpcusername;
        private readonly string _fpcpassword;
        private readonly string _invoiceNinjaToken;
        private readonly string _emailOrders;
        private readonly string _bulkVSusername;
        private readonly string _bulkVSpassword;
        private readonly string _data247username;
        private readonly string _data247password;

        public CartController(IConfiguration config)
        {
            _configuration = config;
            _teleToken = Guid.Parse(config.GetConnectionString("TeleAPI"));
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
            _apiKey = config.GetConnectionString("BulkVSAPIKEY");
            _apiSecret = config.GetConnectionString("BulkVSAPISecret");
            _bulkVSusername = config.GetConnectionString("BulkVSUsername");
            _bulkVSpassword = config.GetConnectionString("BulkVSPassword");
            _fpcusername = config.GetConnectionString("PComNetUsername");
            _fpcpassword = config.GetConnectionString("PComNetPassword");
            _invoiceNinjaToken = config.GetConnectionString("InvoiceNinjaToken");
            _emailOrders = config.GetConnectionString("EmailOrders");
            _data247username = config.GetConnectionString("Data247Username");
            _data247password = config.GetConnectionString("Data247Password");
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> IndexAsync()
        {
            await HttpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(HttpContext.Session);

            return View("Index", new CartResult
            {
                Cart = cart
            });
        }

        [HttpGet]
        [Route("Cart/Checkout")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> CheckoutAsync()
        {
            await HttpContext.Session.LoadAsync().ConfigureAwait(false);
            var cart = Cart.GetFromSession(HttpContext.Session);

            if (cart.ProductOrders.Count() == 0)
            {
                return View("Index", new CartResult { Cart = cart });
            }

            // Create a GUID for an order to prevent multiple order submissions from repeated button clicking.
            cart.Order.OrderId = Guid.NewGuid();

            var checkSet = cart.SetToSession(HttpContext.Session);

            return View("Order", cart);
        }

        // Show orders that have already been submitted.
        [HttpGet("Cart/Order/{Id}")]
        [HttpPost("Cart/Order/{Id}")]
        [ResponseCache(VaryByHeader = "User-Agent", Duration = 30, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> ExistingOrderAsync(Guid Id, bool? AddPortingInfo)
        {
            if (Id != Guid.Empty)
            {
                var order = await Order.GetByIdAsync(Id, _postgresql).ConfigureAwait(false);
                if (order == null || string.IsNullOrWhiteSpace(order.Email))
                {
                    return View("Index", new CartResult
                    {
                        Cart = null,
                        Message = "Couldn't find this order in our system."
                    });
                }

                var productOrders = await ProductOrder.GetAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var purchasedPhoneNumbers = await PurchasedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var verifiedPhoneNumbers = await VerifiedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);
                var portedPhoneNumbers = await PortedPhoneNumber.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                // Rather than using a completely generic concept of a product we have two kind of products: phone number and everything else.
                // This is done for performance because we have 300k phone numbers where the DialedNumber is the primary key and perhaps 20 products where a guid is the key.
                var products = new List<Product>();
                var services = new List<Service>();
                foreach (var item in productOrders)
                {
                    if (item?.ProductId != Guid.Empty)
                    {
                        var product = await Product.GetAsync(item.ProductId, _postgresql).ConfigureAwait(false);
                        products.Add(product);
                    }
                    else if (item?.ServiceId != Guid.Empty)
                    {
                        var service = await Service.GetAsync(item.ServiceId, _postgresql).ConfigureAwait(false);
                        services.Add(service);
                    }
                }

                var cart = new Cart
                {
                    Order = order,
                    PhoneNumbers = new List<PhoneNumber>(),
                    ProductOrders = productOrders,
                    Products = products,
                    Services = services,
                    PortedPhoneNumbers = portedPhoneNumbers,
                    VerifiedPhoneNumbers = verifiedPhoneNumbers,
                    PurchasedPhoneNumbers = purchasedPhoneNumbers
                };

                if (AddPortingInfo != null)
                {
                    var portRequest = await PortRequest.GetByOrderIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                    var checkSet = cart.SetToSession(HttpContext.Session);

                    return View("Success", new OrderWithPorts
                    {
                        Order = order,
                        PortRequest = portRequest,
                        PhoneNumbers = cart.PortedPhoneNumbers
                    });
                }
                else
                {
                    return View("Order", cart);

                }
            }
            else
            {
                return View("Order");
            }
        }

        [HttpPost("Cart/Submit")]
        [ValidateAntiForgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> SubmitAsync(Order order)
        {
            if (order != null && !string.IsNullOrWhiteSpace(order.Email))
            {
                order.DateSubmitted = DateTime.Now;

                await HttpContext.Session.LoadAsync().ConfigureAwait(false);
                var cart = Cart.GetFromSession(HttpContext.Session);

                if (order.OrderId != Guid.Empty)
                {
                    var orderExists = await Order.GetByIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                    // Create a new order.
                    if (orderExists is null)
                    {
                        // Prevent the background work from happening before it's queued up.
                        order.BackgroundWorkCompleted = true;

                        // Save to db.
                        var submittedOrder = await order.PostAsync(_postgresql).ConfigureAwait(false);

                        // Send a confirmation email.
                        if (submittedOrder)
                        {
                            order = await Order.GetByIdAsync(order.OrderId, _postgresql).ConfigureAwait(false);

                            // Submit the number orders and track the total cost.
                            var onetimeItems = new List<Invoice_Items>();
                            var reoccuringItems = new List<Invoice_Items>();
                            var totalCost = 0;

                            // Create a single PIN for this order.
                            var random = new Random();
                            var pin = random.Next(100000, 99999999);

                            foreach (var nto in cart.PhoneNumbers)
                            {
                                var productOrder = cart.ProductOrders.Where(x => x.DialedNumber == nto.DialedNumber).FirstOrDefault();
                                var numberToBePurchased = cart.PhoneNumbers.Where(x => x.DialedNumber == nto.DialedNumber).FirstOrDefault();
                                productOrder.OrderId = order.OrderId;

                                var cost = nto.NumberType == "Executive" ? 200 : nto.NumberType == "Premium" ? 40 : nto.NumberType == "Standard" ? 20 : 20;

                                if (nto.IngestedFrom == "BulkVS")
                                {
                                    var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);
                                    var purchsedNumber = new PurchasedPhoneNumber
                                    {
                                        Completed = false,
                                        DateIngested = numberToBePurchased.DateIngested,
                                        DateOrdered = DateTime.Now,
                                        NPA = numberToBePurchased.NPA,
                                        NXX = numberToBePurchased.NXX,
                                        XXXX = numberToBePurchased.XXXX,
                                        DialedNumber = numberToBePurchased.DialedNumber,
                                        IngestedFrom = numberToBePurchased.IngestedFrom,
                                        NumberType = numberToBePurchased.NumberType,
                                        OrderId = order.OrderId,
                                        OrderResponse = string.Empty,
                                        PIN = pin.ToString()
                                    };

                                    var checkPurchaseOrder = await purchsedNumber.PostAsync(_postgresql).ConfigureAwait(false);

                                    totalCost += cost;

                                    onetimeItems.Add(new Invoice_Items
                                    {
                                        product_key = nto.DialedNumber,
                                        notes = $"{nto.NumberType} Phone Number",
                                        cost = cost,
                                        qty = 1
                                    });
                                }
                                else if (nto.IngestedFrom == "TeleMessage")
                                {
                                    var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);

                                    var purchsedNumber = new PurchasedPhoneNumber
                                    {
                                        Completed = false,
                                        DateIngested = numberToBePurchased.DateIngested,
                                        DateOrdered = DateTime.Now,
                                        NPA = numberToBePurchased.NPA,
                                        NXX = numberToBePurchased.NXX,
                                        XXXX = numberToBePurchased.XXXX,
                                        DialedNumber = numberToBePurchased.DialedNumber,
                                        IngestedFrom = numberToBePurchased.IngestedFrom,
                                        NumberType = numberToBePurchased.NumberType,
                                        OrderId = order.OrderId,
                                        OrderResponse = string.Empty
                                    };

                                    var checkPurchaseOrder = await purchsedNumber.PostAsync(_postgresql).ConfigureAwait(false);

                                    totalCost += cost;

                                    onetimeItems.Add(new Invoice_Items
                                    {
                                        product_key = nto.DialedNumber,
                                        notes = $"{nto.NumberType} Phone Number",
                                        cost = cost,
                                        qty = 1
                                    });
                                }
                                else if (nto.IngestedFrom == "FirstPointCom")
                                {
                                    var checkSubmitted = productOrder.PostAsync(_postgresql).ConfigureAwait(false);

                                    var purchsedNumber = new PurchasedPhoneNumber
                                    {
                                        Completed = false,
                                        DateIngested = numberToBePurchased.DateIngested,
                                        DateOrdered = DateTime.Now,
                                        NPA = numberToBePurchased.NPA,
                                        NXX = numberToBePurchased.NXX,
                                        XXXX = numberToBePurchased.XXXX,
                                        DialedNumber = numberToBePurchased.DialedNumber,
                                        IngestedFrom = numberToBePurchased.IngestedFrom,
                                        NumberType = numberToBePurchased.NumberType,
                                        OrderId = order.OrderId,
                                        OrderResponse = string.Empty
                                    };

                                    var checkPurchaseOrder = await purchsedNumber.PostAsync(_postgresql).ConfigureAwait(false);

                                    totalCost += cost;

                                    onetimeItems.Add(new Invoice_Items
                                    {
                                        product_key = nto.DialedNumber,
                                        notes = $"{nto.NumberType} Phone Number",
                                        cost = cost,
                                        qty = 1
                                    });
                                }
                            }

                            // Track the number of free ports this order qualifies for.
                            var freePortedNumbers = cart.Services.Count();
                            var emailSubject = string.Empty;

                            foreach (var productOrder in cart.ProductOrders)
                            {
                                productOrder.OrderId = order.OrderId;

                                if (!string.IsNullOrWhiteSpace(productOrder.DialedNumber))
                                {
                                    emailSubject = string.IsNullOrWhiteSpace(emailSubject) ? productOrder.DialedNumber : emailSubject;
                                }

                                if (productOrder.PortedPhoneNumberId is not null)
                                {
                                    var ported = cart.PortedPhoneNumbers.Where(x => x.PortedPhoneNumberId == productOrder.PortedPhoneNumberId).FirstOrDefault();

                                    // Discount one ported number for each service they purchase.
                                    var calculatedCost = 20;

                                    if (freePortedNumbers > 0)
                                    {
                                        freePortedNumbers--;
                                        calculatedCost = 0;
                                    }
                                    // If they use up all of their free ports, then charge $2 a line.
                                    else if (freePortedNumbers == 0 && cart.Services.Any())
                                    {
                                        calculatedCost = 2;
                                    }

                                    if (ported != null)
                                    {
                                        totalCost += calculatedCost;
                                        onetimeItems.Add(new Invoice_Items
                                        {
                                            product_key = ported.PortedDialedNumber,
                                            notes = $"Phone Number to Port to our Network",
                                            cost = calculatedCost,
                                            qty = 1
                                        });
                                    }

                                    emailSubject = string.IsNullOrWhiteSpace(emailSubject) ? productOrder.PortedDialedNumber : emailSubject;

                                    var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);
                                }

                                if (productOrder.VerifiedPhoneNumberId is not null)
                                {
                                    var verfied = cart.VerifiedPhoneNumbers.Where(x => x.VerifiedPhoneNumberId == productOrder.VerifiedPhoneNumberId).FirstOrDefault();

                                    if (verfied != null)
                                    {
                                        totalCost += 10;
                                        onetimeItems.Add(new Invoice_Items
                                        {
                                            product_key = verfied.VerifiedDialedNumber,
                                            notes = $"Phone Number to Verify Daily",
                                            cost = 10,
                                            qty = 1
                                        });
                                    }

                                    emailSubject = string.IsNullOrWhiteSpace(emailSubject) ? verfied.VerifiedDialedNumber : emailSubject;

                                    var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);
                                }

                                if (productOrder.ProductId != Guid.Empty)
                                {
                                    var product = cart.Products.Where(x => x.ProductId == productOrder.ProductId).FirstOrDefault();

                                    if (product != null)
                                    {
                                        totalCost += product.Price;
                                        onetimeItems.Add(new Invoice_Items
                                        {
                                            product_key = product.Name,
                                            notes = $"{product.Description}",
                                            cost = product.Price,
                                            qty = productOrder.Quantity
                                        });
                                    }

                                    emailSubject = string.IsNullOrWhiteSpace(emailSubject) ? product.Name : emailSubject;

                                    var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);
                                }

                                if (productOrder.ServiceId != Guid.Empty)
                                {
                                    var service = cart.Services.Where(x => x.ServiceId == productOrder.ServiceId).FirstOrDefault();

                                    if (service != null)
                                    {
                                        totalCost += service.Price;
                                        reoccuringItems.Add(new Invoice_Items
                                        {
                                            product_key = service.Name,
                                            notes = $"{service.Description}",
                                            cost = service.Price,
                                            qty = productOrder.Quantity
                                        });
                                    }

                                    emailSubject = string.IsNullOrWhiteSpace(emailSubject) ? service.Name : emailSubject;

                                    var checkSubmitted = await productOrder.PostAsync(_postgresql).ConfigureAwait(false);
                                }
                            }

                            // Associate the ported numbers with this order.
                            foreach (var portedNumber in cart.PortedPhoneNumbers)
                            {
                                portedNumber.OrderId = order.OrderId;

                                var checkPort = await portedNumber.PostAsync(_postgresql).ConfigureAwait(false);

                                Log.Information($"[Checkout] Saved port request for number {portedNumber.PortedDialedNumber}.");
                            }

                            // Associate the verified numbers with this order.
                            foreach (var verifiedNumber in cart.VerifiedPhoneNumbers)
                            {
                                verifiedNumber.OrderId = order.OrderId;

                                var checkVerified = await verifiedNumber.PostAsync(_postgresql).ConfigureAwait(false);

                                Log.Information($"[Checkout] Saved Verified Number {verifiedNumber.VerifiedDialedNumber} to the Database.");
                            }

                            // Handle the tax information for the invoice and fall back to simplier queries if we get failures.
                            SalesTax specificTaxRate = null;
                            try
                            {
                                specificTaxRate = await SalesTax.GetAsync(order.Address, order.City, order.Zip).ConfigureAwait(false);
                            }
                            catch
                            {
                                Log.Fatal($"[Checkout] Failed to get the Sale Tax rate for {order.Address}, {order.City}, {order.Zip}.");
                            }

                            if (specificTaxRate is null)
                            {
                                try
                                {
                                    specificTaxRate = await SalesTax.GetAsync(string.Empty, order.City, order.Zip).ConfigureAwait(false);
                                }
                                catch
                                {
                                    Log.Fatal($"[Checkout] Failed to get the Sale Tax rate for {order.City}, {order.Zip}.");
                                }
                            }

                            if (specificTaxRate is null)
                            {
                                try
                                {
                                    specificTaxRate = await SalesTax.GetAsync(string.Empty, string.Empty, order.Zip).ConfigureAwait(false);
                                }
                                catch
                                {
                                    Log.Fatal($"[Checkout] Failed to get the Sale Tax rate for {order.Zip}.");
                                }
                            }

                            var billingTaxRate = new TaxRateDatum();

                            if (!(specificTaxRate is null) && !(specificTaxRate.rate is null))
                            {
                                var rateName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(specificTaxRate.rate.name.ToLowerInvariant());
                                var taxRateName = $"{rateName}, WA - {specificTaxRate.loccode}";
                                var taxRateValue = specificTaxRate.rate1 * 100M;

                                var existingTaxRates = await TaxRate.GetAllAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                billingTaxRate = existingTaxRates.data.Where(x => x.name == taxRateName).FirstOrDefault();
                                if (billingTaxRate is null)
                                {
                                    billingTaxRate = new TaxRateDatum
                                    {
                                        name = taxRateName,
                                        rate = taxRateValue
                                    };

                                    var checkCreate = await billingTaxRate.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                }

                                Log.Information($"[Checkout] {billingTaxRate.name} @ {billingTaxRate.rate}.");
                            }
                            else
                            {
                                Log.Information($"[Checkout] Failed to get the Tax Rate from WA State.");
                            }

                            // Create the confirmation email.
                            var confirmationEmail = new Email
                            {
                                PrimaryEmailAddress = order.Email,
                                CarbonCopy = _emailOrders,
                                MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thank you for choosing Accelerate Networks! 
<br />
<br />                                                                            
Your order has been submitted and <a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>can be reviewed here</a>, a delivery specialist will follow up with you soon.
<br />
<br />                                                                            
Thanks,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)",
                                OrderId = order.OrderId,
                                Subject = $"Order confirmation for {emailSubject}"
                            };

                            // Create a billing client and send out an invoice.
                            var billingClients = await Client.GetByEmailAsync(order.Email, _invoiceNinjaToken).ConfigureAwait(false);
                            var billingClient = billingClients.data.FirstOrDefault();
                            if (billingClient is null)
                            {
                                // Create a new client in the billing system.
                                var newBillingClient = new ClientDatum
                                {
                                    name = string.IsNullOrWhiteSpace(order.BusinessName) ? $"{order.FirstName} {order.LastName}" : order.BusinessName,
                                    contacts = new ClientContact[] {
                                        new ClientContact {
                                            email = order.Email,
                                            first_name = order.FirstName,
                                            last_name = order.LastName
                                        }
                                    }
                                };

                                billingClient = await newBillingClient.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                            }

                            // Create the invoices for this order and submit it to the billing system.
                            var upfrontInvoice = new InvoiceDatum
                            {
                                id = billingClient.id,
                                invoice_items = onetimeItems.ToArray(),
                                tax_name1 = billingTaxRate.name,
                                tax_rate1 = billingTaxRate.rate
                            };

                            var reoccuringInvoice = new InvoiceDatum
                            {
                                id = billingClient.id,
                                invoice_items = reoccuringItems.ToArray(),
                                tax_name1 = billingTaxRate.name,
                                tax_rate1 = billingTaxRate.rate
                            };

                            // If they want just a Quote, create a quote in the billing system, not an invoice.
                            if (order.Quote)
                            {
                                // Mark the invoices as quotes.
                                upfrontInvoice.is_quote = true;
                                upfrontInvoice.invoice_type_id = 2;
                                reoccuringInvoice.is_quote = true;
                                reoccuringInvoice.invoice_type_id = 2;

                                // Submit them to the billing system if they have items.
                                if (upfrontInvoice.invoice_items.Any() && reoccuringInvoice.invoice_items.Any())
                                {
                                    var createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    var createNewReoccuringInvoice = await reoccuringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);

                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceReoccuringId = createNewReoccuringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    var oneTimeLink = invoiceLinks.invoices.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;
                                    var reoccuringLink = invoiceLinks.invoices.Where(x => x.id == createNewReoccuringInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                    confirmationEmail.Subject = $"Quote {createNewOneTimeInvoice.invoice_number} and {createNewReoccuringInvoice.invoice_number} from Accelerate Networks";
                                    confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for considering Accelerate Networks, take a look at the <a href={reoccuringLink}'>monthly service cost here</a>, and the <a href='{oneTimeLink}'>upfront cost here</a>.
<br />
<br />                                                                            
Your order has been submitted and <a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>can be reviewed here</a>, a delivery specialist will follow up with you soon.
<br />
<br />                                                                            
Let us know if you have any questions!
<br />
<br />                                                                            
Sincerely,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)";
                                }
                                else if (reoccuringInvoice.invoice_items.Any())
                                {
                                    // Submit them to the billing system.
                                    var createNewReoccuringInvoice = await reoccuringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);

                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewReoccuringInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceReoccuringId = createNewReoccuringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewReoccuringInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    var reoccuringLink = invoiceLinks.invoices.Where(x => x.id == createNewReoccuringInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                    confirmationEmail.Subject = $"Quote {createNewReoccuringInvoice.invoice_number} from Accelerate Networks";
                                    confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for considering Accelerate Networks, take a look at the <a href={reoccuringLink}'>monthly service cost here</a>.
<br />
<br />                                                                            
Your order has been submitted and <a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>can be reviewed here</a>, a delivery specialist will follow up with you soon.
<br />
<br />
Let us know if you have any questions!
<br />
<br />                                                                            
Sincerely,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)";
                                }
                                else if (upfrontInvoice.invoice_items.Any())
                                {
                                    // Submit them to the billing system.
                                    var createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);

                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    var oneTimeLink = invoiceLinks.invoices.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                    confirmationEmail.Subject = $"Quote {createNewOneTimeInvoice.invoice_number} from Accelerate Networks";
                                    confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for considering Accelerate Networks, take a look at the <a href='{oneTimeLink}'>upfront cost here</a>.
<br />
<br />                                                                            
Your order has been submitted and <a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>can be reviewed here</a>, a delivery specialist will follow up with you soon.
<br />
<br />     
Let us know if you have any questions!
<br />
<br />                                                                            
Sincerely,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)";

                                }
                            }
                            else
                            {
                                // Submit them to the billing system if they have items.
                                if (upfrontInvoice.invoice_items.Any() && reoccuringInvoice.invoice_items.Any())
                                {
                                    var createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);
                                    var createNewReoccuringInvoice = await reoccuringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);

                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceReoccuringId = createNewReoccuringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    var oneTimeLink = invoiceLinks.invoices.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;
                                    var reoccuringLink = invoiceLinks.invoices.Where(x => x.id == createNewReoccuringInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                    confirmationEmail.Subject = $"Quote {createNewOneTimeInvoice.invoice_number} and {createNewReoccuringInvoice.invoice_number} from Accelerate Networks";
                                    confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for choosing Accelerate Networks, take a look at the <a href={reoccuringLink}'>monthly service cost here</a>, and the <a href='{oneTimeLink}'>upfront cost here</a>.
<br />
<br />                                                                            
Your order has been submitted and <a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>can be reviewed here</a>, a delivery specialist will follow up with you soon.
<br />
<br />                                                                            
Let us know if you have any questions!
<br />
<br />                                                                            
Sincerely,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)";
                                }
                                else if (reoccuringInvoice.invoice_items.Any())
                                {
                                    var createNewReoccuringInvoice = await reoccuringInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);

                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewReoccuringInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceReoccuringId = createNewReoccuringInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewReoccuringInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    var reoccuringLink = invoiceLinks.invoices.Where(x => x.id == createNewReoccuringInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                    confirmationEmail.Subject = $"Quote {createNewReoccuringInvoice.invoice_number} from Accelerate Networks";
                                    confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for choosing Accelerate Networks, take a look at the <a href={reoccuringLink}'>monthly service cost here</a>.
<br />
<br />                                                                            
Your order has been submitted and <a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>can be reviewed here</a>, a delivery specialist will follow up with you soon.
<br />
<br />                                                                            
Let us know if you have any questions!
<br />
<br />                                                                            
Sincerely,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)";
                                }
                                else if (upfrontInvoice.invoice_items.Any())
                                {
                                    var createNewOneTimeInvoice = await upfrontInvoice.PostAsync(_invoiceNinjaToken).ConfigureAwait(false);

                                    // Update the order with the billing system's client and the two invoice Id's.
                                    order.BillingClientId = createNewOneTimeInvoice.client_id.ToString(CultureInfo.CurrentCulture);
                                    order.BillingInvoiceId = createNewOneTimeInvoice.id.ToString(CultureInfo.CurrentCulture);
                                    var checkQuoteUpdate = await order.PutAsync(_postgresql).ConfigureAwait(false);

                                    var invoiceLinks = await Client.GetByIdWithInoviceLinksAsync(createNewOneTimeInvoice.client_id, _invoiceNinjaToken).ConfigureAwait(false);
                                    var oneTimeLink = invoiceLinks.invoices.Where(x => x.id == createNewOneTimeInvoice.id).FirstOrDefault().invitations.FirstOrDefault().link;

                                    confirmationEmail.Subject = $"Quote {createNewOneTimeInvoice.invoice_number} from Accelerate Networks";
                                    confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />                                                                            
Thanks for choosing Accelerate Networks, take a look at the <a href='{oneTimeLink}'>upfront cost here</a>.
<br />
<br />                                                                            
Your order has been submitted and <a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>can be reviewed here</a>, a delivery specialist will follow up with you soon.
<br />
<br />                                                                            
Let us know if you have any questions!
<br />
<br />                                                                            
Sincerely,
<br />                                                                            
Accelerate Networks
<br />                                                                            
206-858-8757 (call or text)";

                                    // This order is just for purchasing phone numbers.
                                    if (cart.PhoneNumbers.Count() == cart.ProductOrders.Count())
                                    {
                                        var formattedNumbers = string.Empty;

                                        foreach (var item in cart.PhoneNumbers)
                                        {
                                            if (item.IngestedFrom == "BulkVS")
                                            {
                                                formattedNumbers += $"{item.DialedNumber} <strong>PIN: {pin}</strong><br />";
                                            }
                                            else
                                            {
                                                formattedNumbers += $"{item.DialedNumber}<br />";
                                            }
                                        }

                                        if (cart.PhoneNumbers.Count() > 1)
                                        {
                                            confirmationEmail.Subject = $"Order for {cart.PhoneNumbers.FirstOrDefault().DialedNumber} is complete!";
                                            confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />  
The order for {cart.PhoneNumbers.FirstOrDefault().DialedNumber} is ready, let us know if you would like this number to forward to another phone number immediately.
<br />
<br />  
To port these numbers to another provider, please pay <a href='{oneTimeLink}'>this invoice</a> and submit a port out request with your new provider using the following information:
<br />
<br />  
Account number: {cart.PhoneNumbers.FirstOrDefault().DialedNumber}
<br />  
Business Name: {order.BusinessName}
<br />  
Authorized Contact: {order.FirstName} {order.LastName}
<br />  
Address: {order.Address} {order.Address2} {order.City}, {order.State} {order.Zip}
<br />  
<br />  
{formattedNumbers}
<br />  
<a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>Original order</a>
<br />  
<br />  
If you need anything please let us know!
<br />  
<br /> 
Sincerely,
<br /> 
Accelerate Networks
<br /> 
206-858-8757 (call/text)";
                                        }
                                        else
                                        {

                                            confirmationEmail.Subject = $"Order for {cart.PhoneNumbers.FirstOrDefault().DialedNumber} is complete!";
                                            confirmationEmail.MessageBody = $@"Hi {order.FirstName},
<br />
<br />  
The order for {cart.PhoneNumbers.FirstOrDefault().DialedNumber} is ready, let us know if you would like this number to forward to another phone number immediately.
<br />
<br />  
To port this number to another provider, please pay <a href='{oneTimeLink}'>this invoice</a> and submit a port out request with your new provider using the following information:
<br />
<br />  
Account number: {cart.PhoneNumbers.FirstOrDefault().DialedNumber}
<br />  
Business Name: {order.BusinessName}
<br />  
Authorized Contact: {order.FirstName} {order.LastName}
<br />  
Address: {order.Address} {order.Address2} {order.City}, {order.State} {order.Zip}
<br />  
PIN: {pin}
<br />  
<br />  
<a href='https://acceleratenetworks.com/Cart/Order/{order.OrderId}'>Original order</a>
<br />  
<br />  
If you need anything please let us know!
<br />  
<br /> 
Sincerely,
<br /> 
Accelerate Networks
<br /> 
206-858-8757 (call/text)";
                                        }
                                    }
                                }
                            }

                            // If there are notes on the order don't send out any emails.
                            if (string.IsNullOrWhiteSpace(order.CustomerNotes))
                            {
                                // Queue up the confirmation email.
                                confirmationEmail.Completed = false;
                                var checkSave = await confirmationEmail.PostAsync(_postgresql).ConfigureAwait(false);
                            }
                            else
                            {
                                Log.Information($"Skipped sending out the confirmation emails for {order.OrderId} due to customer notes.");
                            }

                            // Allow the background work to commence.
                            order.BackgroundWorkCompleted = false;
                            var checkOrderUpdate = order.PutAsync(_postgresql).ConfigureAwait(false);

                            if (cart.PortedPhoneNumbers.Any())
                            {
                                HttpContext.Session.Clear();

                                return View("Success", new OrderWithPorts
                                {
                                    Order = order,
                                    PhoneNumbers = cart.PortedPhoneNumbers
                                });
                            }
                            else
                            {
                                // Reset the session and clear the Cart.
                                HttpContext.Session.Clear();

                                return View("Success", new OrderWithPorts
                                {
                                    Order = order
                                });
                            }
                        }
                    }
                    // Display an existing order.
                    else
                    {
                        // Reset the session and clear the Cart.
                        HttpContext.Session.Clear();

                        return Redirect($"/Cart/Order/{cart.Order.OrderId}");
                    }
                }

                if (cart.PortedPhoneNumbers.Any())
                {
                    return View("Success", new OrderWithPorts
                    {
                        Order = cart.Order,
                        PhoneNumbers = cart.PortedPhoneNumbers
                    });
                }
                else
                {
                    return RedirectToAction("Cart", "Checkout");
                }
            }
            else
            {
                return RedirectToAction("Cart", "Checkout");
            }
        }
    }
}