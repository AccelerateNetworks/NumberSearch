using System;
using System.Collections.Generic;

#nullable disable

namespace NumberSearch.Ops.EFModels
{
    public partial class PurchasedPhoneNumber
    {
        public Guid PurchasedPhoneNumberId { get; set; }
        public Guid OrderId { get; set; }
        public string DialedNumber { get; set; }
        public string IngestedFrom { get; set; }
        public DateTime DateIngested { get; set; }
        public DateTime DateOrdered { get; set; }
        public string OrderResponse { get; set; }
        public bool Completed { get; set; }
        public int? Npa { get; set; }
        public int? Nxx { get; set; }
        public int? Xxxx { get; set; }
        public string NumberType { get; set; }
        public string Pin { get; set; }
    }
}
