using NumberSearch.DataAccess.Models;

namespace NumberSearch.Mvc.Models
{
    public class SearchResults
    {
        public string Query { get; set; } = string.Empty;
        public string CleanQuery { get; set; } = string.Empty;
        public int NumberOfResults { get; set; }
        public string View { get; set; } = string.Empty;
        public int Page { get; set; }
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string[] Cities { get; set; } = [];
        public PhoneNumber[] PhoneNumbers { get; set; } = [];
        public Cart Cart { get; set; } = new();
    }
}
