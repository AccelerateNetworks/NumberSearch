﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using NumberSearch.Ops.EFModels;
using NumberSearch.Ops.Models;

namespace NumberSearch.Ops.Controllers
{
    public class ProductShipmentsController : Controller
    {
        private readonly numberSearchContext _context;

        public ProductShipmentsController(numberSearchContext context)
        {
            _context = context;
        }

        // GET: ProductShipments
        [Authorize]
        [HttpGet("ProductShipments")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.ProductShipments.ToListAsync());
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

            var productShipment = await _context.ProductShipments
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
        public async Task<IActionResult> Create()
        {
            var products = await _context.Products.ToListAsync();
            var create = new Ops.Models.CreateProductShipment
            {
                Products = products
            };
            return View(create);
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
                productShipment.ProductShipmentId = Guid.NewGuid();
                productShipment.DateCreated = DateTime.Now;
                var products = await _context.Products.ToListAsync();

                if (string.IsNullOrWhiteSpace(productShipment.Name))
                {
                    productShipment.Name = products.Where(x => x.ProductId == productShipment.ProductId).FirstOrDefault().Name;
                }

                // Update all product inventory counts when a shipment is added or updated.
                foreach (var product in products)
                {
                    var relatedShipments = await _context.ProductShipments.Where(x => x.ProductId == product.ProductId).ToListAsync();
                    var instockItems = relatedShipments.Where(x => x.ShipmentType == "Instock").Sum(x => x.Quantity);
                    var assignedItems = relatedShipments.Where(x => x.ShipmentType == "Assigned").Sum(x => x.Quantity);
                    product.QuantityAvailable = instockItems - assignedItems;
                    _context.Update(product);
                }

                _context.Add(productShipment);
                await _context.SaveChangesAsync();
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

            var productShipment = await _context.ProductShipments.FindAsync(id);
            if (productShipment == null)
            {
                return NotFound();
            }
            return View(productShipment);
        }

        // POST: ProductShipments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost("ProductShipments/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("ProductShipmentId,ProductId,OrderId,BillingClientId,Name,ShipmentSource,PurchasePrice,ShipmentType,Quantity,DateCreated")] ProductShipment productShipment)
        {
            if (id != productShipment.ProductShipmentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(productShipment);

                    var products = await _context.Products.ToListAsync();

                    if (string.IsNullOrWhiteSpace(productShipment.Name))
                    {
                        productShipment.Name = products.Where(x => x.ProductId == productShipment.ProductId).FirstOrDefault().Name;
                    }

                    // Update all product inventory counts when a shipment is added or updated.
                    foreach (var product in products)
                    {
                        var relatedShipments = await _context.ProductShipments.Where(x => x.ProductId == product.ProductId).ToListAsync();
                        var instockItems = relatedShipments.Where(x => x.ShipmentType == "Instock").Sum(x => x.Quantity);
                        var assignedItems = relatedShipments.Where(x => x.ShipmentType == "Assigned").Sum(x => x.Quantity);
                        product.QuantityAvailable = instockItems - assignedItems;
                        _context.Update(product);
                    }

                    await _context.SaveChangesAsync();
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
            return View(productShipment);
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

            var productShipment = await _context.ProductShipments
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
            var productShipment = await _context.ProductShipments.FindAsync(id);
            _context.ProductShipments.Remove(productShipment);
            await _context.SaveChangesAsync();

            var products = await _context.Products.ToListAsync();

            // Update all product inventory counts when a shipment is added or updated.
            foreach (var product in products)
            {
                var relatedShipments = await _context.ProductShipments.Where(x => x.ProductId == product.ProductId).ToListAsync();
                var instockItems = relatedShipments.Where(x => x.ShipmentType == "Instock").Sum(x => x.Quantity);
                var assignedItems = relatedShipments.Where(x => x.ShipmentType == "Assigned").Sum(x => x.Quantity);
                product.QuantityAvailable = instockItems - assignedItems;
                _context.Update(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductShipmentExists(Guid id)
        {
            return _context.ProductShipments.Any(e => e.ProductShipmentId == id);
        }
    }
}