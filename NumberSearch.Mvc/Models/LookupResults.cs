using NumberSearch.DataAccess;
using NumberSearch.DataAccess.BulkVS;

using System.Collections.Generic;

namespace NumberSearch.Mvc.Models
{
    public class LookupResults
    {
        public string DialedNumber { get; set; }
        public IEnumerable<LrnBulkCnam> Lookups { get; set; }
        public IEnumerable<PortedPhoneNumber> Portable { get; set; }
        public IEnumerable<PortedPhoneNumber> Wireless { get; set; }
        public IEnumerable<string> NotPortable { get; set; }
        public Cart Cart { get; set; }
        public bool Port { get; set; }
        public string Message { get; set; }
    }
}
