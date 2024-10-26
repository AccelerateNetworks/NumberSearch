using AccelerateNetworks.Operations;
namespace NumberSearch.Ops.Models;

public class CreateCarrier
{
    public Carrier Carrier { get; set; } = new();
    public Carrier[] Carriers { get; set; } = [];
    public PhoneNumberLookup[] Lookups { get; set; } = [];
}