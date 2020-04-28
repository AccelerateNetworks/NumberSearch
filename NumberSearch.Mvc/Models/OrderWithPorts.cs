using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Models;

namespace NumberSearch.Mvc
{
    public class OrderWithPorts
    {
        public Order Order { get; set; }
        public PortRequest PortRequest { get; set; }
        public IEnumerable<PortedPhoneNumber> PhoneNumbers { get; set; }
    }
}
