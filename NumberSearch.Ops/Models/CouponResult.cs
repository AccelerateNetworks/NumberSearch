using AccelerateNetworks.Operations;

using System.Collections.Generic;

namespace NumberSearch.Ops
{
    public class CouponResult
    {
        public Coupon Coupon { get; set; }
        public IEnumerable<Coupon> Coupons { get; set; }
        public string Message { get; set; }
    }
}