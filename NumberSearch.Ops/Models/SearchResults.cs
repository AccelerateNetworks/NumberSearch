namespace NumberSearch.Ops.Models
{
    public class SearchResults
    {
        public string Query { get; set; } = string.Empty;
        public string CleanQuery { get; set; } = string.Empty;
        public int NumberOfResults { get; set; }
        public int Page { get; set; }
        public DataAccess.PhoneNumber[] PhoneNumbers { get; set; } = [];
    }
}
