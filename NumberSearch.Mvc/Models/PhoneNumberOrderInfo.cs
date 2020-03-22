using NumberSearch.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Models
{
    public class PhoneNumberOrderInfo
    {
        public PhoneNumber number { get; set; }
        public PhoneNumberDetail detail { get; set; }
        public PhoneNumberOrder Order { get; set; }
    }
}
