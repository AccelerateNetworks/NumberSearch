using NumberSearch.DataAccess;

namespace NumberSearch.Mvc
{
    public class PortingResults
    {
        public PortedPhoneNumber? PortedPhoneNumber { get; set; }
        public Cart? Cart { get; set; }
        public string? Query { get; set; }
        public string? Message { get; set; }
        public string? AlertType { get; set; }
    }
}
