using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Ops.Models
{
    public class CreateProductShipment
    {
        public IEnumerable<EFModels.Product> Products { get; set; }
        public EFModels.ProductShipment Shipment { get; set; }
    }
}
