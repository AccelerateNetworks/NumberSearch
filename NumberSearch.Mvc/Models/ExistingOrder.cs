using NumberSearch.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc
{
    public class ExistingOrder
    {
        public Order Order { get; set; }
        public IEnumerable<ProductOrder> Items { get; set; }
    }
}
