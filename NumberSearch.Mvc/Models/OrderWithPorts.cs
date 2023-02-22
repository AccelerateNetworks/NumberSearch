using NumberSearch.DataAccess;

using System;

namespace NumberSearch.Mvc
{
    public class OrderWithPorts
    {
        public Order Order { get; set; } = new();
        public PortRequest PortRequest { get; set; } = new();
        public PortedPhoneNumber[] PhoneNumbers { get; set; } = Array.Empty<PortedPhoneNumber>();
    }
}
