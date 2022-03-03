using System.Collections.Generic;

using AccelerateNetworks.Operations;
namespace NumberSearch.Ops.Models;

public class CreateLookup
{
    public IEnumerable<Carrier>? Carriers { get; set; }
    public PhoneNumberLookup? Lookup { get; set; }
}