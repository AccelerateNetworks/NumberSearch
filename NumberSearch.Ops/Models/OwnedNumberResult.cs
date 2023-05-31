using AccelerateNetworks.Operations;

using System;

namespace NumberSearch.Ops
{
    public class OwnedNumberResult
    {
        public OwnedPhoneNumber Owned { get; set; } = new();
        public PortedPhoneNumber[] PortedPhoneNumbers { get; set; } = Array.Empty<PortedPhoneNumber>();
        public PurchasedPhoneNumber[] PurchasedPhoneNumbers { get; set; } = Array.Empty<PurchasedPhoneNumber>();
        public EmergencyInformation EmergencyInformation { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}