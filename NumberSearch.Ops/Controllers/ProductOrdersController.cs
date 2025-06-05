using AccelerateNetworks.Operations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using NumberSearch.Ops.Models;

using System;
using System.Linq;
using System.Threading.Tasks;

using ZLinq;

namespace NumberSearch.Ops.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class ProductOrdersController : Controller
{
    private readonly numberSearchContext _context;
    private readonly IConfiguration _configuration;
    private readonly string _postgresql;

    public ProductOrdersController(numberSearchContext context, IConfiguration config)
    {
        _context = context;
        _configuration = config;
        _postgresql = _configuration.GetConnectionString("PostgresqlProd") ?? string.Empty;
    }

    // GET: ProductOrders
    [Authorize]
    [HttpGet("Order/{orderId}/ProductOrders")]
    public async Task<IActionResult> ProductOrdersByOrder(Guid orderId)
    {
        var products = await _context.Products.ToArrayAsync();
        var services = await _context.Services.ToArrayAsync();
        var coupons = await _context.Coupons.ToArrayAsync();
        var productOrders = await _context.ProductOrders.Where(x => x.OrderId == orderId).ToListAsync();
        return View("Index", new ProductOrderResult { ProductOrders = productOrders, Coupons = coupons, Products = products, Services = services });
    }

    // GET: ProductOrders/Details/5
    [Authorize]
    [HttpGet("ProductOrders/Details/{id}")]
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var productOrder = await _context.ProductOrders
            .FirstOrDefaultAsync(m => m.ProductOrderId == id);
        if (productOrder == null)
        {
            return NotFound();
        }

        return View(productOrder);
    }

    // GET: ProductOrders/Create
    [Authorize]
    [HttpGet("ProductOrders/Create")]
    public async Task<IActionResult> CreateAsync(Guid? orderId)
    {
        if (orderId == null || orderId == Guid.Empty)
        {
            return NotFound();
        }

        var products = await _context.Products.ToArrayAsync();
        var services = await _context.Services.ToArrayAsync();
        var coupons = await _context.Coupons.ToArrayAsync();
        var productOrders = await _context.ProductOrders.Where(x => x.OrderId == orderId).ToListAsync();

        return View("Create", new ProductOrderResult
        {
            ProductOrder = new ProductOrder
            {
                OrderId = orderId ?? Guid.NewGuid(),
                CreateDate = DateTime.Now,
                Quantity = 1
            },
            ProductOrders = productOrders,
            Products = products,
            Services = services,
            Coupons = coupons
        });
    }

    // POST: ProductOrders/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [Authorize]
    [HttpPost("ProductOrders/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ProductOrderId,OrderId,ProductId,ServiceId,DialedNumber,PortedDialedNumber,Quantity,CreateDate,PortedPhoneNumberId,VerifiedPhoneNumberId,CouponId")] ProductOrder productOrder)
    {
        if (productOrder.ProductOrderId != Guid.Empty && productOrder.OrderId != Guid.Empty && productOrder.CreateDate > DateTime.MinValue)
        {
            if (!string.IsNullOrWhiteSpace(productOrder.DialedNumber))
            {
                var checkNumber = PhoneNumbersNA.PhoneNumber.TryParse(productOrder.DialedNumber, out var number);

                if (checkNumber)
                {
                    var checkExists = await _context.ProductOrders.Where(x => x.OrderId == productOrder.OrderId && x.DialedNumber == number.DialedNumber).FirstOrDefaultAsync();

                    if (checkExists is null)
                    {
                        var purchased = new DataAccess.PurchasedPhoneNumber
                        {
                            DialedNumber = number.DialedNumber ?? string.Empty,
                            Completed = true,
                            DateIngested = DateTime.Now,
                            DateOrdered = DateTime.Now,
                            IngestedFrom = "Ops",
                            NPA = number.NPA,
                            NXX = number.NXX,
                            XXXX = number.XXXX,
                            NumberType = "Standard",
                            OrderId = productOrder.OrderId,
                            OrderResponse = string.Empty,
                            PIN = string.Empty,
                            PurchasedPhoneNumberId = Guid.NewGuid()
                        };

                        var checkPurchased = await purchased.PostAsync(_postgresql);

                        productOrder.DialedNumber = number.DialedNumber;
                        productOrder.Quantity = 1;

                        _context.Add(productOrder);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(productOrder.PortedDialedNumber))
            {
                var checkNumber = PhoneNumbersNA.PhoneNumber.TryParse(productOrder.PortedDialedNumber, out var number);

                if (checkNumber)
                {
                    var checkExists = await _context.ProductOrders.Where(x => x.OrderId == productOrder.OrderId && x.PortedDialedNumber == number.DialedNumber).FirstOrDefaultAsync();

                    if (checkExists is null)
                    {
                        var portRequestsController = new PortRequestsController(_configuration, _context);

                        var port = await portRequestsController.VerifyPortabilityAsync(number.DialedNumber ?? string.Empty);

                        if (port.Portable)
                        {
                            var portRequest = await DataAccess.PortRequest.GetByOrderIdAsync(productOrder.OrderId, _postgresql);

                            if (portRequest is not null)
                            {
                                port.PortRequestId = portRequest.PortRequestId;
                            }

                            port.OrderId = productOrder.OrderId;
                            _context.PortedPhoneNumbers.Add(port);
                            await _context.SaveChangesAsync();
                        }

                        productOrder.PortedDialedNumber = number.DialedNumber;
                        productOrder.PortedPhoneNumberId = port.PortedPhoneNumberId;
                        productOrder.Quantity = 1;
                        _context.Add(productOrder);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            else if (productOrder.ServiceId.HasValue)
            {
                var checkService = await _context.ProductOrders.Where(x => x.OrderId == productOrder.OrderId && x.ServiceId == productOrder.ServiceId).FirstOrDefaultAsync();

                if (checkService is null)
                {
                    _context.Add(productOrder);
                    await _context.SaveChangesAsync();
                }
            }
            else if (productOrder.ProductId.HasValue)
            {
                var checkProduct = await _context.ProductOrders.Where(x => x.OrderId == productOrder.OrderId && x.ProductId == productOrder.ProductId).FirstOrDefaultAsync();

                if (checkProduct is null)
                {
                    _context.Add(productOrder);
                    await _context.SaveChangesAsync();
                }
            }
            else if (productOrder.CouponId.HasValue)
            {
                var checkCoupon = await _context.ProductOrders.Where(x => x.OrderId == productOrder.OrderId && x.CouponId == productOrder.CouponId).FirstOrDefaultAsync();

                if (checkCoupon is null)
                {
                    _context.Add(productOrder);
                    await _context.SaveChangesAsync();
                }
            }

            return Redirect($"/Home/Order/{productOrder.OrderId}");
        }
        else
        {
            var products = await _context.Products.ToArrayAsync();
            var services = await _context.Services.ToArrayAsync();
            var coupons = await _context.Coupons.ToArrayAsync();
            var productOrders = await _context.ProductOrders.Where(x => x.OrderId == productOrder.OrderId).ToListAsync();

            return View("Create", new ProductOrderResult { ProductOrder = new ProductOrder { OrderId = productOrder.OrderId, CreateDate = DateTime.Now }, ProductOrders = productOrders, Products = products, Services = services, Coupons = coupons });
        }
    }

    // GET: ProductOrders/Edit/5
    [Authorize]
    [HttpGet("ProductOrders/Edit/{id}")]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var productOrder = await _context.ProductOrders.FindAsync(id);
        if (productOrder == null)
        {
            return NotFound();
        }
        return View(productOrder);
    }

    // POST: ProductOrders/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [Authorize]
    [HttpPost("ProductOrders/Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("ProductOrderId,OrderId,ProductId,ServiceId,DialedNumber,PortedDialedNumber,Quantity,CreateDate,PortedPhoneNumberId,VerifiedPhoneNumberId,CouponId")] ProductOrder productOrder)
    {
        if (id != productOrder.ProductOrderId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(productOrder);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductOrderExists(productOrder.ProductOrderId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return Redirect($"/Home/Order/{productOrder.OrderId}");
        }
        return Redirect($"/Home/Order/{productOrder.OrderId}");
    }

    // GET: ProductOrders/Delete/5
    [Authorize]
    [HttpGet("ProductOrders/Delete/{id}")]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var productOrder = await _context.ProductOrders
            .FirstOrDefaultAsync(m => m.ProductOrderId == id);
        if (productOrder == null)
        {
            return NotFound();
        }

        return View(productOrder);
    }

    // POST: ProductOrders/Delete/5
    [Authorize]
    [HttpPost("ProductOrders/Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var productOrder = await _context.ProductOrders.FindAsync(id);
        if (productOrder is not null)
        {
            _context.ProductOrders.Remove(productOrder);
            await _context.SaveChangesAsync();
        }

        // Delete the child items as well as the parent product order.
        if (productOrder is not null && !string.IsNullOrWhiteSpace(productOrder.DialedNumber))
        {
            var purchasedNumber = await DataAccess.PurchasedPhoneNumber.GetByDialedNumberAndOrderIdAsync(productOrder.DialedNumber, productOrder.OrderId, _postgresql);

            if (purchasedNumber is not null)
            {
                var checkDelete = purchasedNumber.DeleteAsync(_postgresql);
            }
        }
        else if (productOrder is not null && !string.IsNullOrWhiteSpace(productOrder.PortedDialedNumber))
        {
            var portedNumbers = await DataAccess.PortedPhoneNumber.GetByOrderIdAsync(productOrder.OrderId, _postgresql);
            var portedNumber = portedNumbers.AsValueEnumerable().Where(x => x.PortedDialedNumber == productOrder.PortedDialedNumber).FirstOrDefault();

            if (portedNumber is not null)
            {
                var checkDelete = await portedNumber.DeleteAsync(_postgresql);
            }
        }

        return Redirect($"/Home/Order/{productOrder?.OrderId}");
    }

    private bool ProductOrderExists(Guid id)
    {
        return _context.ProductOrders.Any(e => e.ProductOrderId == id);
    }
}