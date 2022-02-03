using AccelerateNetworks.Operations;

namespace NumberSearch.Ops.Models
{
    public class EditOrderResult
    {
        public Order Order { get; set; }
        public Cart Cart { get; set; }
        public ProductShipment[] ProductShipments { get; set; }
        public string Message { get; set; }
        public string AlertType { get; set; }
    }
}
