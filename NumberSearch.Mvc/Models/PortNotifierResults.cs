using NumberSearch.DataAccess;

namespace NumberSearch.Mvc
{
    public class PortNotifierResults
    {
        public VerifiedPhoneNumber VerifiedPhoneNumber { get; set; } = new();
        public Cart Cart { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
    }
}
