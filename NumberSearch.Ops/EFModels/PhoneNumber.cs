using System;
using System.Collections.Generic;

#nullable disable

namespace NumberSearch.Ops.EFModels
{
    public partial class PhoneNumber
    {
        public string DialedNumber { get; set; }
        public int Npa { get; set; }
        public int Nxx { get; set; }
        public int Xxxx { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string IngestedFrom { get; set; }
        public DateTime DateIngested { get; set; }
        public string NumberType { get; set; }
        public bool Purchased { get; set; }
    }
}
