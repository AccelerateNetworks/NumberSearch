using AccelerateNetworks.Operations;

using System;

using static NumberSearch.Ops.Controllers.OwnedNumbersController;

namespace NumberSearch.Ops
{
    public class OwnedNumberResult
    {
        public OwnedPhoneNumber Owned { get; set; } = new();
        public PortedPhoneNumber[] PortedPhoneNumbers { get; set; } = Array.Empty<PortedPhoneNumber>();
        public PurchasedPhoneNumber[] PurchasedPhoneNumbers { get; set; } = Array.Empty<PurchasedPhoneNumber>();
        public EmergencyInformation EmergencyInformation { get; set; } = new();
        public Order[] RelatedOrders { get; set; } = Array.Empty<Order>();
        public ClientRegistration ClientRegistration { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string UnparsedAddress { get; set; } = string.Empty;
    }
}