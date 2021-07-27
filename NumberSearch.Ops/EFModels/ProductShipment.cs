using System;
using System.Collections.Generic;

#nullable disable

namespace NumberSearch.Ops.EFModels
{
    public partial class ProductShipment
    {
        public Guid ProductShipmentId { get; set; }
        public Guid? ProductId { get; set; }
        public Guid? OrderId { get; set; }
        public string BillingClientId { get; set; }
        public string Name { get; set; }
        public string ShipmentSource { get; set; }
        public decimal? PurchasePrice { get; set; }
        public string ShipmentType { get; set; }
        public int Quantity { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
