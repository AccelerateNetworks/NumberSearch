using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc
{
    public class PortingResults
    {
        public PortedPhoneNumber PortedPhoneNumber { get; set; }
        public Cart Cart { get; set; }
    }
}
