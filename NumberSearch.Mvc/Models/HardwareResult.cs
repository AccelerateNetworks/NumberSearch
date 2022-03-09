using NumberSearch.DataAccess;

using System.Collections.Generic;

namespace NumberSearch.Mvc
{
    public class HardwareResult
    {
        public Cart? Cart { get; set; }

        public Product[]? Phones { get; set; }

        public Product[]? Accessories { get; set; }
    }
}
