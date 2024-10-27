using AccelerateNetworks.Operations;

namespace NumberSearch.Ops.Models
{
    public class PurchasedResult
    {
        public PurchasedPhoneNumber PurchasedPhoneNumber { get; set; } = new();
        public PurchasedPhoneNumber[] PurchasedPhoneNumbers { get; set; } = [];
        public OwnedPhoneNumber[] Owned { get; set; } = [];
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
    }
}
