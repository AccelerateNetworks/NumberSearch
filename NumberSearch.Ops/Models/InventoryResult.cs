using AccelerateNetworks.Operations;

namespace NumberSearch.Ops.Models
{
    public class InventoryResult
    {
        public ProductShipment Shipment { get; set; } = new();
        public Product Product { get; set; } = new();
        public ProductShipment[] ProductShipments { get; set; } = [];
        public Product[] Products { get; set; } = [];
        public string Message { get; set; } = string.Empty;
    }
}
