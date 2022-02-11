using System;

namespace AccelerateNetworks.Operations
{
    public class ProductItem
    {
        public Guid ProductItemId { get; set; }
        public Guid ProductId { get; set; }
        public Guid? ProductShipmentId { get; set; }
        public Guid? OrderId { get; set; }
        public string SerialNumber { get; set; }
        public string MACAddress { get; set; }
        public string Condition { get; set; }
        public string ExternalOrderId { get; set; }
        public string ShipmentTrackingLink { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
