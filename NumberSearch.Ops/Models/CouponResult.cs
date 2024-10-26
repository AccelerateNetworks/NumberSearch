using AccelerateNetworks.Operations;

namespace NumberSearch.Ops.Models
{
    public class CouponResult
    {
        public Coupon Coupon { get; set; } = new();
        public Coupon[] Coupons { get; set; } = [];
        public string Message { get; set; } = string.Empty;
    }
}