using NumberSearch.DataAccess;

using System;

namespace NumberSearch.Mvc
{
    public class HardwareResult
    {
        public Cart Cart { get; set; } = new();
        public Product[] Phones { get; set; } = Array.Empty<Product>();
        public Product[] Accessories { get; set; } = Array.Empty<Product>();
    }
}
