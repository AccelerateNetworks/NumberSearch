using AccelerateNetworks.Operations;

using System;

using static NumberSearch.Ops.Controllers.OwnedNumbersController;

namespace NumberSearch.Ops
{
    // For the edit single page
    public class OwnedNumberResult
    {
        public OwnedPhoneNumber Owned { get; set; } = new();
        public PortedPhoneNumber[] PortedPhoneNumbers { get; set; } = [];
        public PurchasedPhoneNumber[] PurchasedPhoneNumbers { get; set; } = [];
        public EmergencyInformation EmergencyInformation { get; set; } = new();
        public Order[] RelatedOrders { get; set; } = [];
        public ClientRegistration ClientRegistration { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string UnparsedAddress { get; set; } = string.Empty;
        public string AddressUnitType { get; set; } = string.Empty;
        public string AddressUnitNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
    }
    // For the list all page
    public class OwnedNumberResultForm
    {
        public OwnedNumberResult[] Results { get; set; } = [];
        public string CarrierName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
    }
}