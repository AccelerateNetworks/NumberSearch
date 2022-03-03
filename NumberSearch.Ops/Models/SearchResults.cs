using AccelerateNetworks.Operations;

namespace NumberSearch.Ops
{
    public class SearchResults
    {
        public string? Query { get; set; }
        public string? CleanQuery { get; set; }
        public int NumberOfResults { get; set; }
        public int Page { get; set; }
        public DataAccess.PhoneNumber[]? PhoneNumbers { get; set; }
    }
}
