using NumberSearch.DataAccess;

using System.Collections.Generic;

namespace NumberSearch.Ops
{
    public class PortRequestResult
    {
        public Order Order { get; set; }
        public PortRequest PortRequest { get; set; }
        public IEnumerable<PortedPhoneNumber> PhoneNumbers { get; set; }
    }
}
