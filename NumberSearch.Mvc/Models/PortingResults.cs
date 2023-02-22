using NumberSearch.DataAccess;

namespace NumberSearch.Mvc
{
    public class PortingResults
    {
        public PortedPhoneNumber PortedPhoneNumber { get; set; } = new();
        public Cart Cart { get; set; } = new();
        public string Query { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
    }
}
