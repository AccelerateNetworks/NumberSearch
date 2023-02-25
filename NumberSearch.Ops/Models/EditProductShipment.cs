using AccelerateNetworks.Operations;

using System;

namespace NumberSearch.Ops.Models;

public class EditProductShipment
{
    public ProductItem[] ProductItems { get; set; } = Array.Empty<ProductItem>();
    public ProductShipment Shipment { get; set; } = new();
}