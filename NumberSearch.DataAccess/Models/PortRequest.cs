using Microsoft.AspNetCore.Http;

using System;
using System.Collections.Generic;
using System.Text;

namespace NumberSearch.DataAccess
{
    public class PortRequest
    {
        public Guid OrderId { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string BillingPhone { get; set; }
        public string LocationType { get; set; }
        public string BusinessContact { get; set; }
        public string BusinessName { get; set; }
        public string ProviderAccountNumber { get; set; }
        public string ProviderPIN { get; set; }
        public bool PartialPort { get; set; }
        public string PartialPortDescription { get; set; }
        public bool WirelessNumber { get; set; }
        public string CallerId { get; set; }
        public IFormFile BillImage { get; set; }
        public DateTime DateSubmitted { get; set; }
    }
}
