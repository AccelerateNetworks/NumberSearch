using AccelerateNetworks.Operations;
namespace NumberSearch.Ops.Models;

public class CreateLookup
{
    public Carrier[] Carriers { get; set; } = [];
    public PhoneNumberLookup Lookup { get; set; } = new();
}