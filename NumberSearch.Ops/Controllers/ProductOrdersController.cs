using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NumberSearch.Ops.EFModels;

namespace NumberSearch.Ops.Controllers
{
    public class ProductOrdersController : Controller
    {
        private readonly numberSearchContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProductOrdersController(numberSearchContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ProductOrders
        [Authorize]
        [HttpGet("ProductOrders")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.ProductOrders.ToListAsync());
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
        public IActionResult Create()
        {
            return View();
        }

        // POST: ProductOrders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost("ProductOrders/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductOrderId,OrderId,ProductId,ServiceId,DialedNumber,PortedDialedNumber,Quantity,CreateDate,PortedPhoneNumberId,VerifiedPhoneNumberId,CouponId")] ProductOrder productOrder)
        {
            if (ModelState.IsValid)
            {
                productOrder.ProductOrderId = Guid.NewGuid();
                _context.Add(productOrder);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(productOrder);
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
                return RedirectToAction(nameof(Index));
            }
            return View(productOrder);
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
            _context.ProductOrders.Remove(productOrder);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductOrderExists(Guid id)
        {
            return _context.ProductOrders.Any(e => e.ProductOrderId == id);
        }
    }
}
