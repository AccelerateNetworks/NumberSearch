using AccelerateNetworks.Operations;

using System;

namespace NumberSearch.Ops
{
    public class PortRequestResult
    {
        public Order Order { get; set; } = new();
        public PortRequest PortRequest { get; set; } = new();
        public PortedPhoneNumber[] PhoneNumbers { get; set; } = Array.Empty<PortedPhoneNumber>();
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
    }
}
