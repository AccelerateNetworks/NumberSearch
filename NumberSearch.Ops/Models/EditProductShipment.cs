using AccelerateNetworks.Operations;

namespace NumberSearch.Ops.Models;

public class EditProductShipment
{
    public ProductItem[] ProductItems { get; set; } = [];
    public ProductShipment Shipment { get; set; } = new();
}