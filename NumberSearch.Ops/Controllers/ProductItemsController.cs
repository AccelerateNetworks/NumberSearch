using AccelerateNetworks.Operations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers
{
    public class ProductItemsController : Controller
    {
        private readonly numberSearchContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly string _postgresql;

        public ProductItemsController(numberSearchContext context, UserManager<IdentityUser> userManager, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _configuration = config;
            _postgresql = _configuration.GetConnectionString("PostgresqlProd");
        }

        // GET: ProductItems
        [Authorize]
        [HttpGet("ProductItems")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.ProductItems.ToListAsync());
        }

        // GET: ProductItems/Details/5
        [Authorize]
        [HttpGet("ProductItems/Details/{id}")]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productItem = await _context.ProductItems
                .FirstOrDefaultAsync(m => m.ProductItemId == id);
            if (productItem == null)
            {
                return NotFound();
            }

            return View(productItem);
        }

        // GET: ProductItems/Create
        [Authorize]
        [HttpGet("ProductItems/Create")]
        [HttpGet("ProductItems/Create/{shipmentId}")]
        public async Task<IActionResult> Create(Guid? shipmentId)
        {
            if (shipmentId is not null)
            {
                var shipment = await _context.ProductShipments.FirstOrDefaultAsync(x => x.ProductShipmentId == shipmentId);
                var countExistingItems = await _context.ProductItems.Where(x => x.ProductShipmentId == shipment.ProductShipmentId).CountAsync();
                var itemsToCreate = countExistingItems != shipment.Quantity ? shipment.Quantity - countExistingItems : 0;

                if (itemsToCreate > 0)
                {
                    for (var i = 0; i < shipment.Quantity; i++)
                    {
                        var item = new ProductItem
                        {
                            ProductShipmentId = shipment.ProductShipmentId,
                            OrderId = shipment.OrderId,
                            ProductId = shipment?.ProductId ?? Guid.Empty,
                            ProductItemId = Guid.NewGuid(),
                        };

                        _context.ProductItems.Add(item);
                    }

                    _context.SaveChanges();
                }

                return Redirect($"/ProductShipments/Edit/{shipment.ProductShipmentId}");
            }
            else
            {
                return View();
            }
        }

        // POST: ProductItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost("ProductItems")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductItemId,ProductId,ProductShipmentId,OrderId,SerialNumber,MACAddress,Condition,ExternalOrderId,ShipmentTrackingLink,DateCreated,DateUpdated")] ProductItem productItem)
        {
            if (ModelState.IsValid)
            {
                productItem.ProductItemId = Guid.NewGuid();
                _context.Add(productItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(productItem);
        }

        // GET: ProductItems/Edit/5
        [Authorize]
        [HttpGet("ProductItems/Edit/{id}")]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productItem = await _context.ProductItems.FindAsync(id);
            if (productItem == null)
            {
                return NotFound();
            }
            return View(productItem);
        }

        // POST: ProductItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost("ProductItems/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("ProductItemId,ProductId,ProductShipmentId,OrderId,SerialNumber,MACAddress,Condition,ExternalOrderId,ShipmentTrackingLink,DateCreated,DateUpdated")] ProductItem productItem)
        {
            if (id != productItem.ProductItemId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(productItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductItemExists(productItem.ProductItemId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return Redirect($"/Home/Order/{productItem?.OrderId}");
            }
            return View(productItem);
        }

        // GET: ProductItems/Delete/5
        [Authorize]
        [HttpGet("ProductItems/Delete/{id}")]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productItem = await _context.ProductItems
                .FirstOrDefaultAsync(m => m.ProductItemId == id);
            if (productItem == null)
            {
                return NotFound();
            }

            return View(productItem);
        }

        // POST: ProductItems/Delete/5
        [Authorize]
        [HttpPost("ProductItems/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var productItem = await _context.ProductItems.FindAsync(id);
            _context.ProductItems.Remove(productItem);
            await _context.SaveChangesAsync();
            if (productItem?.OrderId is not null)
            {
                return Redirect($"/Home/Order/{productItem?.OrderId}");
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ProductItemExists(Guid id)
        {
            return _context.ProductItems.Any(e => e.ProductItemId == id);
        }
    }
}
