using System;
using System.Collections.Generic;

namespace AccelerateNetworks.Operations
{
    public partial class PurchasedPhoneNumber
    {
        public Guid PurchasedPhoneNumberId { get; set; }
        public Guid OrderId { get; set; }
        public string? DialedNumber { get; set; }
        public string IngestedFrom { get; set; } = null!;
        public DateTime DateIngested { get; set; }
        public DateTime DateOrdered { get; set; }
        public string? OrderResponse { get; set; }
        public bool Completed { get; set; }
        public int? NPA { get; set; }
        public int? NXX { get; set; }
        public int? XXXX { get; set; }
        public string? NumberType { get; set; }
        public string? Pin { get; set; }
    }
}
