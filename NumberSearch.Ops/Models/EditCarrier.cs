using AccelerateNetworks.Operations;

using System.Collections.Generic;

namespace NumberSearch.Ops.Models
{
    public class EditCarrier
    {
        public IEnumerable<PhoneNumberLookup> Lookups { get; set; }
        public Carrier Carrier { get; set; }
    }
}