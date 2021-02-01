using NumberSearch.DataAccess;

using System.Collections.Generic;

namespace NumberSearch.Ops
{
    public class InventoryResult
    {
        public ProductShipment Shipment { get; set; }
        public Product Product { get; set; }
        public IEnumerable<ProductShipment> ProductShipments { get; set; }
        public IEnumerable<Product> Products { get; set; }
        public string Message { get; set; }
    }
}
