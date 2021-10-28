using System.Collections.Generic;

namespace NumberSearch.Ops.Models
{
    public class CreateLookup
    {
        public IEnumerable<EFModels.Carrier> Carriers { get; set; }
        public EFModels.PhoneNumberLookup Lookup { get; set; }
    }
}
