using NumberSearch.DataAccess;
using System.Collections.Generic;

namespace NumberSearch.Mvc.Models
{
    public class SearchResults
    {
        public string Query { get; set; }
        public string CleanQuery { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "I like arrays")]
        public PhoneNumber[] PhoneNumbers { get; set; }
    }
}
