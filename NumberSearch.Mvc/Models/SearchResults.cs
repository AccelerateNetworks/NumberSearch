using NumberSearch.DataAccess;

using System.Collections.Generic;

namespace NumberSearch.Mvc
{
    public class SearchResults
    {
        public string Query { get; set; }
        public string CleanQuery { get; set; }
        public int NumberOfResults { get; set; }
        public string View { get; set; }
        public int Page { get; set; }
        public string Message { get; set; }
        public string AlertType { get; set; }
        public string City { get; set; }
        public IEnumerable<string> Cities { get; set; }
        public IEnumerable<PhoneNumber> PhoneNumbers { get; set; }
        public Cart Cart { get; set; }
    }
}
