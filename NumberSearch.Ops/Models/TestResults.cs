using NumberSearch.DataAccess;

using ServiceReference;

using System.Collections.Generic;

namespace NumberSearch.Ops
{
    public class TestResults
    {
        public string NPA { get; set; }
        public string NXX { get; set; }
        public string DialedNumber { get; set; }
        public IEnumerable<PhoneNumber> PhoneNumbersFPC { get; set; }
        public IEnumerable<PhoneNumber> PhoneNumbersTM { get; set; }
        public IEnumerable<PhoneNumber> PhoneNumbersBVS { get; set; }

        public QueryResult PhoneNumberOrder { get; set; }
        public LrnLookup LRNLookup { get; set; }
        public string PortabilityResponse { get; set; }
    }
}
