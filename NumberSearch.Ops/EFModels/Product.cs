using System;
using System.Collections.Generic;

#nullable disable

namespace NumberSearch.Ops.EFModels
{
    public partial class Product
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; }
        public string Price { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public bool Public { get; set; }
        public int QuantityAvailable { get; set; }
        public string SupportLink { get; set; }
        public int? DisplayPriority { get; set; }
        public string VendorPartNumber { get; set; }
        public string Type { get; set; }
    }
}
