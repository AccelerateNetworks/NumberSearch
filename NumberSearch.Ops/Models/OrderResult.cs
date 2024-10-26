using AccelerateNetworks.Operations;

namespace NumberSearch.Ops.Models
{
    public class OrderResult
    {
        public OrderProducts[] Orders { get; set; } = [];
        public Product[] Products { get; set; } = [];
        public Service[] Services { get; set; } = [];
        public PortedPhoneNumber[] PortedPhoneNumbers { get; set; } = [];
        public PurchasedPhoneNumber[] PurchasedPhoneNumbers { get; set; } = [];
        public VerifiedPhoneNumber[] VerifiedPhoneNumbers { get; set; } = [];
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
    }

    public class OrderProducts
    {
        public Order Order { get; set; } = new();
        public PortRequest PortRequest { get; set; } = new();
        public ProductOrder[] ProductOrders { get; set; } = [];
    }
}
