using AccelerateNetworks.Operations;

namespace NumberSearch.Ops.Models
{
    public class EditOrderResult
    {
        public Order Order { get; set; } = new();
        public Cart Cart { get; set; } = new();
        public ProductItem[] ProductItems { get; set; } = [];
        public PortRequest PortRequest { get; set; } = new();
        public EmergencyInformation[] EmergencyInformation { get; set; } = [];
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
    }
}
