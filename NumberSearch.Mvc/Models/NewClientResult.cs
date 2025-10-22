using NumberSearch.DataAccess;

namespace NumberSearch.Mvc.Models
{
    public class NewClientResult
    {
        public NewClient NewClient { get; set; } = new();
        public Order Order { get; set; } = new();
        public ProductOrder[] ProductOrders { get; set; } = [];
        public Product[] Products { get; set; } = [];
        public string[] PhoneNumbers { get; set; } = [];
    }
}
