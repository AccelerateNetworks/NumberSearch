using NumberSearch.DataAccess;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.Mvc.Models
{
    public class NewClientResult
    {
        public NewClient NewClient { get; set; }
        public Order Order { get; set; }
    }
}
