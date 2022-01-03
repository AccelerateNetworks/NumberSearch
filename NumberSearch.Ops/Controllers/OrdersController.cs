using AccelerateNetworks.Operations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers;
public class OrdersController : Controller
{
    private readonly numberSearchContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public OrdersController(numberSearchContext context,
        UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Orders
    [Authorize]
    [HttpGet("Orders")]
    public async Task<IActionResult> Index()
    {
        // Show only the relevant Orders to a Sales rep.
        if (User.IsInRole("Sales"))
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            if (user is not null)
            {
                return View(
                    await _context.Orders
                    .Where(x => (x.Quote != true)
                    && (x.SalesEmail == user.Email)
                ).ToListAsync());
            }

            return View(await _context.Orders.Where(x => x.Quote != true).ToListAsync());
        }
        else
        {
            return View(await _context.Orders.Where(x => x.Quote != true).ToListAsync());
        }
    }

    [Authorize]
    [HttpGet("Quotes")]
    public async Task<IActionResult> QuotesIndex()
    {
        return View("Index", await _context.Orders.Where(x => x.Quote == true).ToListAsync());
    }

    // GET: Orders/Details/5
    [Authorize]
    [HttpGet("Orders/Details/{id}")]
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var order = await _context.Orders
            .FirstOrDefaultAsync(m => m.OrderId == id);
        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    // GET: Orders/Create
    [Authorize]
    [HttpGet("Orders/Create")]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Orders/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [Authorize]
    [HttpPost("Orders/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("OrderId,FirstName,LastName,Email,Address,Address2,City,State,Zip,DateSubmitted,BusinessName,CustomerNotes,BillingClientId,BillingInvoiceId,Quote,BillingInvoiceReoccuringId,SalesEmail,BackgroundWorkCompleted,Completed,InstallDate,UpfrontInvoiceLink,ReoccuringInvoiceLink,OnsiteInstallation,AddressUnitType,AddressUnitNumber,UnparsedAddress")] Order order)
    {
        if (ModelState.IsValid)
        {
            order.OrderId = Guid.NewGuid();
            _context.Add(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(order);
    }

    // GET: Orders/Edit/5
    [Authorize]
    [HttpGet("Orders/Edit/{id}")]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }
        return View(order);
    }

    // POST: Orders/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [Authorize]
    [HttpPost("Orders/Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("OrderId,FirstName,LastName,Email,Address,Address2,City,State,Zip,DateSubmitted,BusinessName,CustomerNotes,BillingClientId,BillingInvoiceId,Quote,BillingInvoiceReoccuringId,SalesEmail,BackgroundWorkCompleted,Completed,InstallDate,UpfrontInvoiceLink,ReoccuringInvoiceLink,OnsiteInstallation,AddressUnitType,AddressUnitNumber,UnparsedAddress")] Order order)
    {
        if (id != order.OrderId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(order);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(order.OrderId))
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
        return View(order);
    }

    // GET: Orders/Delete/5
    [Authorize]
    [HttpGet("Orders/Delete/{id}")]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var order = await _context.Orders
            .FirstOrDefaultAsync(m => m.OrderId == id);
        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    // POST: Orders/Delete/5
    [Authorize]
    [HttpPost("Orders/Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool OrderExists(Guid id)
    {
        return _context.Orders.Any(e => e.OrderId == id);
    }
}