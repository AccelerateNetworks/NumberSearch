using AccelerateNetworks.Operations;

using System.Collections.Generic;

namespace NumberSearch.Ops.Models;

public class EditProductShipment
{
    public IEnumerable<ProductItem> ProductItems { get; set; }
    public ProductShipment Shipment { get; set; }
}