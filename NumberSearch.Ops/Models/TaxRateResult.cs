using NumberSearch.DataAccess.InvoiceNinja;

namespace NumberSearch.Ops
{
    public class TaxRateResult
    {
        public string Address { get; set; }
        public string City { get; set; }
        public string Zip { get; set; }
        public TaxRate Rates { get; set; }
        public string Message { get; set; }
    }
}
