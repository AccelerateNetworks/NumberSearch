namespace NumberSearch.Mvc.Models
{
    public class SearchResults
    {
        public string Query { get; set; }
        public string CleanQuery { get; set; }
        public PhoneNumber[] PhoneNumbers { get; set; }
    }
}
