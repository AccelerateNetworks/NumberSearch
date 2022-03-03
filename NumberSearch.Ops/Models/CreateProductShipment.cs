using AccelerateNetworks.Operations;

using System.Collections.Generic;

namespace NumberSearch.Ops.Models;

public class CreateProductShipment
{
    public IEnumerable<Product>? Products { get; set; }
    public ProductShipment? Shipment { get; set; }
}