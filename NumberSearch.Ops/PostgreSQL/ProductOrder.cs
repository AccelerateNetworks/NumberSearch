using System;
using System.Collections.Generic;

namespace AccelerateNetworks.Operations
{
    public partial class ProductOrder
    {
        public Guid OrderId { get; set; }
        public Guid? ProductId { get; set; }
        public Guid? ServiceId { get; set; }
        public string? DialedNumber { get; set; }
        public string? PortedDialedNumber { get; set; }
        public long Quantity { get; set; }
        public DateTime CreateDate { get; set; }
        public Guid? PortedPhoneNumberId { get; set; }
        public Guid? VerifiedPhoneNumberId { get; set; }
        public Guid? CouponId { get; set; }
        public Guid ProductOrderId { get; set; }
    }
}
