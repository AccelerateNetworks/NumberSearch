using NumberSearch.Mvc.Models;

namespace NumberSearch.Mvc
{
    public class ServicesResult
    {
        public Cart Cart { get; set; } = new();
        public string Type { get; set; } = string.Empty;
    }
}
