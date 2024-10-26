using AccelerateNetworks.Operations;

namespace NumberSearch.Ops.Models;

public class CreateProductShipment
{
    public Product[] Products { get; set; } = [];
    public ProductShipment Shipment { get; set; } = new();
}