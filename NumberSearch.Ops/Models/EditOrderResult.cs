using AccelerateNetworks.Operations;

using System;

namespace NumberSearch.Ops.Models
{
    public class EditOrderResult
    {
        public Order Order { get; set; } = new();
        public Cart Cart { get; set; } = new();
        public ProductItem[] ProductItems { get; set; } = Array.Empty<ProductItem>();
        public PortRequest PortRequest { get; set; } = new();
        public EmergencyInformation EmergencyInformation { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
    }
}
