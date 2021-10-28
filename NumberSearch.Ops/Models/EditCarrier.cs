using System.Collections.Generic;

namespace NumberSearch.Ops.Models
{
    public class EditCarrier
    {
        public IEnumerable<EFModels.PhoneNumberLookup> Lookups { get; set; }
        public EFModels.Carrier Carrier { get; set; }
    }
}