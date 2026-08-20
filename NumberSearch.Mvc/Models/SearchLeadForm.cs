namespace NumberSearch.Mvc.Models
{
    public class SearchLeadForm
    {
        public string Name { get; set; } = string.Empty;
        public string ContactPhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string View { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
    }
}
