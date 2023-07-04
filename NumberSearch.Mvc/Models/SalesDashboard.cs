using NumberSearch.DataAccess;

using System;

namespace NumberSearch.Mvc
{
    public class SalesDashboard
    {
        public Order[] Orders { get; set; } = Array.Empty<Order>();
    }
}