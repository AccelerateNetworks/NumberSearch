using NumberSearch.DataAccess;

using System.Collections.Generic;

namespace NumberSearch.Ops
{
    public class OrderResult
    {
        public IEnumerable<OrderProducts> Orders { get; set; }
        public IEnumerable<Product> Products { get; set; }
        public IEnumerable<Service> Services { get; set; }
        public IEnumerable<PurchasedPhoneNumber> PurchasedPhoneNumbers { get; set; }
        public IEnumerable<VerifiedPhoneNumber> VerifiedPhoneNumbers { get; set; }
    }

    public class OrderProducts
    {
        public Order Order { get; set; }
        public PortRequest PortRequest { get; set; }
        public IEnumerable<ProductOrder> ProductOrders { get; set; }
    }
}
