using AccelerateNetworks.Operations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class ProductsController : Controller
{
    private readonly numberSearchContext _context;

    public ProductsController(numberSearchContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet("/Products")]
    // GET: Products
    public async Task<IActionResult> Index()
    {
        return View(await _context.Products.ToListAsync());
    }

    [Authorize]
    [HttpGet("Products/Details/{id}")]
    // GET: Products/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(m => m.ProductId == id);
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    [Authorize]
    [HttpGet("/Products/Create")]
    // GET: Products/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Products/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("Products/Create")]
    public async Task<IActionResult> Create([Bind("ProductId,Name,Price,Description,Image,Public,QuantityAvailable,SupportLink,DisplayPriority,VendorPartNumber,Type,Tags,VendorDescription,VendorFeatures")] Product product)
    {
        if (ModelState.IsValid)
        {
            product.ProductId = Guid.NewGuid();
            _context.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(product);
    }

    // GET: Products/Edit/5
    [Authorize]
    [HttpGet("/Products/Edit/{id}")]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }

    // POST: Products/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("/Products/Edit/{id}")]
    public async Task<IActionResult> Edit(Guid id, [Bind("ProductId,Name,Price,Description,Image,Public,QuantityAvailable,SupportLink,DisplayPriority,VendorPartNumber,Type,Tags,VendorDescription,VendorFeatures")] Product product)
    {
        if (id != product.ProductId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.ProductId))
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
        return View(product);
    }

    // GET: Products/Delete/5
    [Authorize]
    [HttpGet("/Products/Delete/{id}")]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(m => m.ProductId == id);
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    // POST: Products/Delete/5
    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("/Products/Delete/{id}")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is not null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool ProductExists(Guid id)
    {
        return _context.Products.Any(e => e.ProductId == id);
    }
}