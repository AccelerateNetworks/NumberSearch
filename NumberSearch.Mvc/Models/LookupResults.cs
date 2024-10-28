using NumberSearch.DataAccess;

using System;

namespace NumberSearch.Mvc.Models
{
    public class LookupResults
    {
        public string DialedNumber { get; set; } = string.Empty;
        public PortedPhoneNumber[] Portable { get; set; } = Array.Empty<PortedPhoneNumber>();
        public PortedPhoneNumber[] Wireless { get; set; } = Array.Empty<PortedPhoneNumber>();
        public string[] NotPortable { get; set; } = Array.Empty<string>();
        public Cart Cart { get; set; } = new();
        public bool Port { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
