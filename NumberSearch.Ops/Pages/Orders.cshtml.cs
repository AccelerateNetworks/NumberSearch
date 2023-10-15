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

                // Handle GUIDs
                if (Query.Length is 36 && Guid.TryParse(Query, out var guidOutput))
                {
                    orders = orders.Where(x => x.OrderId == guidOutput).ToList();
                }

                if (orders.Count > 1)
                {
                    var searchResults = orders.Where(x => x.BusinessName != null
                    && x.BusinessName.ToLowerInvariant().Contains(Query.ToLowerInvariant()))
                        .ToList();

                    // First Name
                    searchResults.AddRange(orders.Where(x => !string.IsNullOrWhiteSpace(x.FirstName)
                                    && x.FirstName.ToLowerInvariant().Contains(Query.ToLowerInvariant())));
                    // Last Name
                    searchResults.AddRange(orders.Where(x => !string.IsNullOrWhiteSpace(x.LastName)
                                    && x.LastName.ToLowerInvariant().Contains(Query.ToLowerInvariant())));
                    // First and Last Name
                    searchResults.AddRange(orders.Where(x => !string.IsNullOrWhiteSpace(x.FirstName)
                                    && !string.IsNullOrWhiteSpace(x.LastName)
                                    && $"{x.FirstName} {x.LastName}".ToLowerInvariant().Contains(query.ToLowerInvariant())));

                    // Phone Number
                    searchResults.AddRange(orders.Where(x => !string.IsNullOrWhiteSpace(x.ContactPhoneNumber)
                                    && x.ContactPhoneNumber.ToLowerInvariant().Contains(query.ToLowerInvariant())));

                    orders = searchResults.Distinct().ToList();
                }
            }

            var portRequests = await _context.PortRequests.AsNoTracking().ToArrayAsync();
            var productOrders = await _context.ProductOrders.AsNoTracking().ToArrayAsync();
            var purchasedNumbers = await _context.PurchasedPhoneNumbers.AsNoTracking().ToArrayAsync();
            var verifiedNumbers = await _context.VerifiedPhoneNumbers.AsNoTracking().ToArrayAsync();
            var portedPhoneNumbers = await _context.PortedPhoneNumbers.AsNoTracking().ToArrayAsync();
            var products = await _context.Products.AsNoTracking().ToArrayAsync();
            var services = await _context.Services.AsNoTracking().ToArrayAsync();
            var pairs = new List<OrderProducts>();

            foreach (var order in orders.OrderByDescending(x => x.DateSubmitted))
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
