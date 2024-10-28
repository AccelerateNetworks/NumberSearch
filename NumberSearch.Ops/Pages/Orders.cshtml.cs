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
    public class OrdersModel(numberSearchContext context,
    UserManager<IdentityUser> userManager) : PageModel
    {
        public string Query { get; set; } = string.Empty;
        public OrderProducts[] Orders { get; set; } = [];
        public Product[] Products { get; set; } = [];
        public Service[] Services { get; set; } = [];
        public PortedPhoneNumber[] PortedPhoneNumbers { get; set; } = [];
        public PurchasedPhoneNumber[] PurchasedPhoneNumbers { get; set; } = [];
        public VerifiedPhoneNumber[] VerifiedPhoneNumbers { get; set; } = [];

        public async Task OnGetAsync(string query)
        {

            // Show all orders
            var orders = new List<Order>();

            // Show only the relevant Orders to a Sales rep.
            if (User.IsInRole("Sales") && !User.IsInRole("Support"))
            {
                var user = await userManager.FindByNameAsync(User.Identity?.Name ?? string.Empty);

                if (user is not null)
                {
                    orders = await context.Orders
                        .Where(x => (x.Quote != true) && (x.SalesEmail == user.Email))
                        .OrderByDescending(x => x.DateSubmitted)
                        .AsNoTracking()
                        .ToListAsync();
                }
                else
                {
                    orders = await context.Orders
                        .Where(x => x.Quote != true)
                        .OrderByDescending(x => x.DateSubmitted)
                        .AsNoTracking()
                        .ToListAsync();
                }
            }
            else
            {
                orders = await context.Orders
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
                    && x.BusinessName.Contains(Query, StringComparison.InvariantCultureIgnoreCase))
                        .ToList();

                    // First Name
                    searchResults.AddRange(orders.Where(x => !string.IsNullOrWhiteSpace(x.FirstName)
                                    && x.FirstName.Contains(Query, StringComparison.InvariantCultureIgnoreCase)));
                    // Last Name
                    searchResults.AddRange(orders.Where(x => !string.IsNullOrWhiteSpace(x.LastName)
                                    && x.LastName.Contains(Query, StringComparison.InvariantCultureIgnoreCase)));
                    // First and Last Name
                    searchResults.AddRange(orders.Where(x => !string.IsNullOrWhiteSpace(x.FirstName)
                                    && !string.IsNullOrWhiteSpace(x.LastName)
                                    && $"{x.FirstName} {x.LastName}".Contains(query, StringComparison.InvariantCultureIgnoreCase)));

                    // Phone Number
                    searchResults.AddRange(orders.Where(x => !string.IsNullOrWhiteSpace(x.ContactPhoneNumber)
                                    && x.ContactPhoneNumber.Contains(query, StringComparison.InvariantCultureIgnoreCase)));

                    orders = searchResults.Distinct().ToList();
                }
            }

            var portRequests = await context.PortRequests.AsNoTracking().ToArrayAsync();
            var productOrders = await context.ProductOrders.AsNoTracking().ToArrayAsync();
            var purchasedNumbers = await context.PurchasedPhoneNumbers.AsNoTracking().ToArrayAsync();
            var verifiedNumbers = await context.VerifiedPhoneNumbers.AsNoTracking().ToArrayAsync();
            var portedPhoneNumbers = await context.PortedPhoneNumbers.AsNoTracking().ToArrayAsync();
            var products = await context.Products.AsNoTracking().ToArrayAsync();
            var services = await context.Services.AsNoTracking().ToArrayAsync();
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

            Orders = [.. pairs];
            Products = products;
            Services = services;
            PortedPhoneNumbers = portedPhoneNumbers;
            PurchasedPhoneNumbers = purchasedNumbers;
            VerifiedPhoneNumbers = verifiedNumbers;
        }
    }
}
