using System;

namespace AccelerateNetworks.Operations
{
    public class ProductItem
    {
        public Guid ProductItemId { get; set; }
        public Guid ProductId { get; set; }
        public Guid? ProductShipmentId { get; set; }
        public Guid? OrderId { get; set; }
        public string? SerialNumber { get; set; } = null!;
        public string? MACAddress { get; set; } = null!;
        public string? Condition { get; set; } = null!;
        public string? ExternalOrderId { get; set; } = null!;
        public string? ShipmentTrackingLink { get; set; } = null!;
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
