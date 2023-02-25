using AccelerateNetworks.Operations;

using System;

namespace NumberSearch.Ops
{
    public class InventoryResult
    {
        public ProductShipment Shipment { get; set; } = new();
        public Product Product { get; set; } = new();
        public ProductShipment[] ProductShipments { get; set; } = Array.Empty<ProductShipment>();
        public Product[] Products { get; set; } = Array.Empty<Product>();
        public string Message { get; set; } = string.Empty;
    }
}
