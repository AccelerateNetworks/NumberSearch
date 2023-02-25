using AccelerateNetworks.Operations;

using System;
using System.Collections.Generic;

namespace NumberSearch.Ops.Models;

public class ProductOrderResult
{
    public ProductOrder ProductOrder { get; set; } = new();
    public List<ProductOrder> ProductOrders { get; set; } = new();
    public Product[] Products { get; set; } = Array.Empty<Product>();
    public Service[] Services { get; set; } = Array.Empty<Service>();
    public Coupon[] Coupons { get; set; } = Array.Empty<Coupon>();
}