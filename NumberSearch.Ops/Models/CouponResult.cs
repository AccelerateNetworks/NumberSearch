using AccelerateNetworks.Operations;

using System;

namespace NumberSearch.Ops
{
    public class CouponResult
    {
        public Coupon Coupon { get; set; } = new();
        public Coupon[] Coupons { get; set; } = Array.Empty<Coupon>();
        public string Message { get; set; } = string.Empty;
    }
}