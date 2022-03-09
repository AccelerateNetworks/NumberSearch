using NumberSearch.DataAccess;

namespace NumberSearch.Mvc
{
    public class PortNotifierResults
    {
        public VerifiedPhoneNumber? VerifiedPhoneNumber { get; set; }
        public Cart? Cart { get; set; }
        public string? Message { get; set; }
        public string? AlertType { get; set; }
    }
}
