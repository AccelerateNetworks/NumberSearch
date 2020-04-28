using System;
using System.Collections.Generic;
using System.Text;

namespace NumberSearch.DataAccess.Models
{
    public class PortRequest
    {
        public string DialedNumber { get; set; }
        public Guid OrderId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public DateTime DateSubmitted { get; set; }
    }
}
