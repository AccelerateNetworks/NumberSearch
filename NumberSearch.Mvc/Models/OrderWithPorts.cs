using NumberSearch.DataAccess;

namespace NumberSearch.Mvc
{
    public class OrderWithPorts
    {
        public Order Order { get; set; } = new();
        public PortRequest PortRequest { get; set; } = new();
        public PortedPhoneNumber[] PhoneNumbers { get; set; } = [];
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
    }
}
