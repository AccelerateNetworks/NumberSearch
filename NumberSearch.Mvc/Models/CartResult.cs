using NumberSearch.Mvc.Models;

namespace NumberSearch.Mvc
{
    public class CartResult
    {
        public Cart Cart { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
    }
}