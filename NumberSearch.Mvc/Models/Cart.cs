using NumberSearch.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc
{
    public class Cart
    {
        public IEnumerable<PhoneNumber> PhoneNumbers { get; set; }
        public IEnumerable<Product> Products { get; set; }
        public Order Order { get; set; }
    }
}
