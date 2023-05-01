using AccelerateNetworks.Operations;

using System;

namespace NumberSearch.Ops.Models
{
    public class EditCarrier
    {
        public PhoneNumberLookup[] Lookups { get; set; } = Array.Empty<PhoneNumberLookup>();
        public Carrier Carrier { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}