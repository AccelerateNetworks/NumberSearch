using AccelerateNetworks.Operations;

using System;

namespace NumberSearch.Ops.Models;

public class CreateProductShipment
{
    public Product[] Products { get; set; } = Array.Empty<Product>();
    public ProductShipment Shipment { get; set; } = new();
}