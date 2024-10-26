using AccelerateNetworks.Operations;

namespace NumberSearch.Ops.Models
{
    public class EditCarrier
    {
        public PhoneNumberLookup[] Lookups { get; set; } = [];
        public Carrier Carrier { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}