using NumberSearch.DataAccess.InvoiceNinja;

namespace NumberSearch.Ops.Models
{
    public class TaxRateResult
    {
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public TaxRate Rates { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}
