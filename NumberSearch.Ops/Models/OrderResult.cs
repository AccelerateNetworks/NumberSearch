using AccelerateNetworks.Operations;

using System;

namespace NumberSearch.Ops
{
    public class OrderResult
    {
        public OrderProducts[] Orders { get; set; } = Array.Empty<OrderProducts>();
        public Product[] Products { get; set; } = Array.Empty<Product>();
        public Service[] Services { get; set; } = Array.Empty<Service>();
        public PortedPhoneNumber[] PortedPhoneNumbers { get; set; } = Array.Empty<PortedPhoneNumber>();
        public PurchasedPhoneNumber[] PurchasedPhoneNumbers { get; set; } = Array.Empty<PurchasedPhoneNumber>();
        public VerifiedPhoneNumber[] VerifiedPhoneNumbers { get; set; } = Array.Empty<VerifiedPhoneNumber>();
    }

    public class OrderProducts
    {
        public Order Order { get; set; } = new();
        public PortRequest PortRequest { get; set; } = new();
        public ProductOrder[] ProductOrders { get; set; } = Array.Empty<ProductOrder>();
    }
}
