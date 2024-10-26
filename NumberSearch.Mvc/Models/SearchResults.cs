using NumberSearch.DataAccess;
using NumberSearch.Mvc.Models;

using System;

namespace NumberSearch.Mvc
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
        public string[] Cities { get; set; } = Array.Empty<string>();
        public PhoneNumber[] PhoneNumbers { get; set; } = Array.Empty<PhoneNumber>();
        public Cart Cart { get; set; } = new();
    }
}
