using NumberSearch.DataAccess;

namespace NumberSearch.Mvc.Models
{
    public class LookupResults
    {
        public string DialedNumber { get; set; } = string.Empty;
        public PortedPhoneNumber[] Portable { get; set; } = [];
        public PortedPhoneNumber[] Wireless { get; set; } = [];
        public string[] NotPortable { get; set; } = [];
        public Cart Cart { get; set; } = new();
        public bool Port { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
