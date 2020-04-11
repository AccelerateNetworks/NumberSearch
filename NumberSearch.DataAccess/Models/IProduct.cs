using System;
using System.Collections.Generic;
using System.Text;

namespace NumberSearch.DataAccess
{
    public interface IProduct
    {
        public Guid ProductId { get; set; }
        public string DialedNumber { get; set; }
    }
}
