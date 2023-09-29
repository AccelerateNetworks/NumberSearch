using AccelerateNetworks.Operations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using NumberSearch.Ops.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Pages
{
    [Authorize]
    public class OrdersModel : PageModel
    {
        private readonly numberSearchContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly OpsConfig _config;
        public string Query { get; set; } = string.Empty;
        public OrderProducts[] Orders { get; set; } = Array.Empty<OrderProducts>();
        public Product[] Products { get; set; } = Array.Empty<Product>();
        public Service[] Services { get; set; } = Array.Empty<Service>();
        public PortedPhoneNumber[] PortedPhoneNumbers { get; set; } = Array.Empty<PortedPhoneNumber>();
        public PurchasedPhoneNumber[] PurchasedPhoneNumbers { get; set; } = Array.Empty<PurchasedPhoneNumber>();
        public VerifiedPhoneNumber[] VerifiedPhoneNumbers { get; set; } = Array.Empty<VerifiedPhoneNumber>();

        public OrdersModel(OpsConfig opsConfig,
        numberSearchContext context,
        UserManager<IdentityUser> userManager)
        {
            _config = opsConfig;
            _context = context;
            _userManager = userManager;
        }

        public async Task OnGetAsync(string query)
        {
            // Show all orders
            var orders = new List<Order>();
            var portRequests = await _context.PortRequests.AsNoTracking().ToArrayAsync();
            var productOrders = await _context.ProductOrders.AsNoTracking().ToArrayAsync();
            var purchasedNumbers = await _context.PurchasedPhoneNumbers.AsNoTracking().ToArrayAsync();
            var verifiedNumbers = await _context.VerifiedPhoneNumbers.AsNoTracking().ToArrayAsync();
            var portedPhoneNumbers = await _context.PortedPhoneNumbers.AsNoTracking().ToArrayAsync();
            var products = await _context.Products.AsNoTracking().ToArrayAsync();
            var services = await _context.Services.AsNoTracking().ToArrayAsync();
            var pairs = new List<OrderProducts>();

            // Show only the relevant Orders to a Sales rep.
            if (User.IsInRole("Sales") && !User.IsInRole("Support"))
            {
                var user = await _userManager.FindByNameAsync(User.Identity?.Name ?? string.Empty);

                if (user is not null)
                {
                    orders = await _context.Orders
                        .Where(x => (x.Quote != true) && (x.SalesEmail == user.Email))
                        .OrderByDescending(x => x.DateSubmitted)
                        .AsNoTracking()
                        .ToListAsync();
                }
                else
                {
                    orders = await _context.Orders
                        .Where(x => x.Quote != true)
                        .OrderByDescending(x => x.DateSubmitted)
                        .AsNoTracking()
                        .ToListAsync();
                }
            }
            else
            {
                orders = await _context.Orders
                    .Where(x => x.Quote != true)
                    .OrderByDescending(x => x.DateSubmitted)
                    .AsNoTracking()
                    .ToListAsync();
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                Query = query;
                orders = orders.Where(x => x.BusinessName != null && x.BusinessName.Contains(Query)).ToList();
            }

            foreach (var order in orders)
            {
                var orderProductOrders = productOrders.Where(x => x.OrderId == order.OrderId).ToArray();
                var portRequest = portRequests.Where(x => x.OrderId == order.OrderId).FirstOrDefault();

                pairs.Add(new OrderProducts
                {
                    Order = order,
                    PortRequest = portRequest ?? new PortRequest(),
                    ProductOrders = orderProductOrders
                });
            }

            Orders = pairs.ToArray();
            Products = products;
            Services = services;
            PortedPhoneNumbers = portedPhoneNumbers;
            PurchasedPhoneNumbers = purchasedNumbers;
            VerifiedPhoneNumbers = verifiedNumbers;
        }
    }
}
