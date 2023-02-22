using NumberSearch.DataAccess;

using System;

namespace NumberSearch.Mvc.Models
{
    public class NewClientResult
    {
        public NewClient NewClient { get; set; } = new();
        public Order Order { get; set; } = new();
        public ProductOrder[] ProductOrders { get; set; } = Array.Empty<ProductOrder>();
        public Product[] Products { get; set; } = Array.Empty<Product>();
        public string[] PhoneNumbers { get; set; } = Array.Empty<string>();
    }
}
