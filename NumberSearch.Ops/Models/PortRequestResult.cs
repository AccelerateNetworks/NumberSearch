using NumberSearch.DataAccess;

using System.Collections.Generic;

namespace NumberSearch.Ops
{
    public class PortRequestResult
    {
        public PortRequest Request { get; set; }
        public IEnumerable<PortedPhoneNumber> Numbers { get; set; }
    }
}
