using NumberSearch.DataAccess;
using NumberSearch.Mvc.Models;

namespace NumberSearch.Mvc
{
    public class HardwareResult
    {
        public Cart Cart { get; set; } = new();
        public Product[] Phones { get; set; } = [];
        public Product[] Accessories { get; set; } = [];

        public Product Product { get; set; } = new();
    }
}
