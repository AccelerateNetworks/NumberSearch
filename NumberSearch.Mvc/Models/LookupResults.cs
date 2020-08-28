using NumberSearch.DataAccess;

using System.Collections.Generic;

namespace NumberSearch.Mvc.Models
{
    public class LookupResults
    {
        public string DialedNumber { get; set; }
        public IEnumerable<LrnLookup> Lookups { get; set; }
    }
}
