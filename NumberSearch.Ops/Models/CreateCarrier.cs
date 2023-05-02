using AccelerateNetworks.Operations;

using System;
namespace NumberSearch.Ops.Models;

public class CreateCarrier
{
    public Carrier Carrier { get; set; } = new();
    public Carrier[] Carriers { get; set; } = Array.Empty<Carrier>();
    public PhoneNumberLookup[] Lookups { get; set; } = Array.Empty<PhoneNumberLookup>();
}