using NumberSearch.DataAccess;

using System.Collections.Generic;

namespace NumberSearch.Mvc
{
    public class HardwareResult
    {
        public Cart Cart { get; set; }

        public IEnumerable<Product> Products { get; set; }
    }
}
