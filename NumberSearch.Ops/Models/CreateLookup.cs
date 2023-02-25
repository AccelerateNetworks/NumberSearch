using AccelerateNetworks.Operations;

using System;
namespace NumberSearch.Ops.Models;

public class CreateLookup
{
    public Carrier[] Carriers { get; set; } = Array.Empty<Carrier>();
    public PhoneNumberLookup Lookup { get; set; } = new();
}