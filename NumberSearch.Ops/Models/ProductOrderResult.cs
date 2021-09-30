using NumberSearch.Ops.EFModels;

using System.Collections.Generic;

namespace NumberSearch.Ops.Models
{
    public class ProductOrderResult
    {
        public List<ProductOrder> ProductOrders { get; set; }
        public Product[] Products { get; set; }
        public Service[] Services { get; set; }
        public Coupon[] Coupons { get; set; }
    }
}