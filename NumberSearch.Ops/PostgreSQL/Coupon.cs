using System;
using System.Collections.Generic;

namespace AccelerateNetworks.Operations
{
    public partial class Coupon
    {
        public Guid CouponId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool Public { get; set; }
        public string? Type { get; set; }
        public int Value { get; set; }
    }
}
