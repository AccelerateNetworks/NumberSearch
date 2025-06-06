﻿using AccelerateNetworks.Operations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NumberSearch.Ops.Models;

using System;
using System.Linq;
using System.Threading.Tasks;

using ZLinq;

namespace NumberSearch.Ops.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class ProductShipmentsController(numberSearchContext context) : Controller
{

    // GET: ProductShipments
    [Authorize]
    [HttpGet("ProductShipments")]
    public async Task<IActionResult> Index()
    {
        return View(await context.ProductShipments.ToListAsync());
    }

    // GET: ProductShipments/Details/5
    [Authorize]
    [HttpGet("ProductShipments/Details/{id}")]
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var productShipment = await context.ProductShipments
            .FirstOrDefaultAsync(m => m.ProductShipmentId == id);
        if (productShipment == null)
        {
            return NotFound();
        }

        return View(productShipment);
    }

    // GET: ProductShipments/Create
    [Authorize]
    [HttpGet("ProductShipments/Create")]
    public async Task<IActionResult> Create(Guid? orderId)
    {
        var products = await context.Products.ToArrayAsync();

        if (orderId is not null)
        {
            var order = await context.Orders.FirstOrDefaultAsync(m => m.OrderId == orderId);

            if (order is not null)
            {
                return View(new Ops.Models.CreateProductShipment
                {
                    Products = products,
                    Shipment = new ProductShipment
                    {
                        OrderId = order.OrderId,
                        BillingClientId = order.BillingClientId,
                        ShipmentType = "Assigned"
                    }
                });
            }
        }

        return View(new Ops.Models.CreateProductShipment
        {
            Products = products
        });
    }

    // POST: ProductShipments/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [Authorize]
    [HttpPost("ProductShipments/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProductShipment productShipmentContainer)
    {
        if (ModelState.IsValid)
        {
            var productShipment = productShipmentContainer.Shipment;

            if (productShipment is not null)
            {
                productShipment.ProductShipmentId = Guid.NewGuid();
                productShipment.DateCreated = DateTime.Now;
                var products = await context.Products.ToListAsync();

                if (string.IsNullOrWhiteSpace(productShipment.Name))
                {
                    productShipment.Name = products.AsValueEnumerable().Where(x => x.ProductId == productShipment.ProductId).FirstOrDefault()?.Name;
                }

                // Update all product inventory counts when a shipment is added or updated.
                foreach (var product in products)
                {
                    var relatedShipments = await context.ProductShipments.Where(x => x.ProductId == product.ProductId).ToListAsync();
                    var instockItems = relatedShipments.AsValueEnumerable().Where(x => x.ShipmentType == "Instock").Sum(x => x.Quantity);
                    var assignedItems = relatedShipments.AsValueEnumerable().Where(x => x.ShipmentType == "Assigned").Sum(x => x.Quantity);
                    product.QuantityAvailable = instockItems - assignedItems;
                    context.Update(product);
                }

                context.Add(productShipment);
                await context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        return View(productShipmentContainer.Shipment);
    }

    // GET: ProductShipments/Edit/5
    [Authorize]
    [HttpGet("ProductShipments/Edit/{id}")]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var productShipment = await context.ProductShipments.FindAsync(id);
        if (productShipment == null)
        {
            return NotFound();
        }
        var productItems = await context.ProductItems.Where(x => x.ProductShipmentId == productShipment.ProductShipmentId).ToArrayAsync();
        return View(new EditProductShipment { ProductItems = productItems, Shipment = productShipment });
    }

    // POST: ProductShipments/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [Authorize]
    [HttpPost("ProductShipments/Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditProductShipment editProductShipment)
    {
        var productShipment = editProductShipment.Shipment;

        if (productShipment is null || id != productShipment.ProductShipmentId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                context.Update(productShipment);

                var products = await context.Products.ToListAsync();

                if (string.IsNullOrWhiteSpace(productShipment.Name))
                {
                    productShipment.Name = products.AsValueEnumerable().Where(x => x.ProductId == productShipment.ProductId).FirstOrDefault()?.Name;
                }

                // Update all product inventory counts when a shipment is added or updated.
                foreach (var product in products)
                {
                    var relatedShipments = await context.ProductShipments.Where(x => x.ProductId == product.ProductId).ToListAsync();
                    var instockItems = relatedShipments.AsValueEnumerable().Where(x => x.ShipmentType == "Instock").Sum(x => x.Quantity);
                    var assignedItems = relatedShipments.AsValueEnumerable().Where(x => x.ShipmentType == "Assigned").Sum(x => x.Quantity);
                    product.QuantityAvailable = instockItems - assignedItems;
                    context.Update(product);
                }

                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductShipmentExists(productShipment.ProductShipmentId))
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

        var productItems = await context.ProductItems.Where(x => x.ProductShipmentId == productShipment.ProductShipmentId).ToArrayAsync();
        return View(new EditProductShipment { ProductItems = productItems, Shipment = productShipment });
    }

    // GET: ProductShipments/Delete/5
    [Authorize]
    [HttpGet("ProductShipments/Delete/{id}")]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var productShipment = await context.ProductShipments
            .FirstOrDefaultAsync(m => m.ProductShipmentId == id);
        if (productShipment == null)
        {
            return NotFound();
        }

        return View(productShipment);
    }

    // POST: ProductShipments/Delete/5
    [Authorize]
    [HttpPost("ProductShipments/Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var productShipment = await context.ProductShipments.FindAsync(id);

        if (productShipment is not null)
        {
            context.ProductShipments.Remove(productShipment);
            await context.SaveChangesAsync();
        }

        var products = await context.Products.ToListAsync();

        // Update all product inventory counts when a shipment is added or updated.
        foreach (var product in products)
        {
            var relatedShipments = await context.ProductShipments.Where(x => x.ProductId == product.ProductId).ToListAsync();
            var instockItems = relatedShipments.AsValueEnumerable().Where(x => x.ShipmentType == "Instock").Sum(x => x.Quantity);
            var assignedItems = relatedShipments.AsValueEnumerable().Where(x => x.ShipmentType == "Assigned").Sum(x => x.Quantity);
            product.QuantityAvailable = instockItems - assignedItems;
            context.Update(product);
        }

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ProductShipmentExists(Guid id)
    {
        return context.ProductShipments.Any(e => e.ProductShipmentId == id);
    }
}