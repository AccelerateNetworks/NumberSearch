using System;
using System.Collections.Generic;
using System.Linq;

namespace NumberSearch.DataAccess
{
    /// <summary>
    /// Fiber internet is $15/mo less per connection when it's bundled with phone service, or when the Partner coupon is applied.
    /// </summary>
    public static class InternetBundle
    {
        public static readonly Guid FiberInternet300ServiceId = new("cbcf5128-5164-40de-8dff-e71d0f152cab");
        public static readonly Guid FiberInternet1GServiceId = new("708c3885-6dab-4a60-9e42-05cf13530076");
        public static readonly Guid StandardLinesServiceId = new("16e2c639-445b-4ae6-9925-07300318206b");
        public static readonly Guid ConcurrentSeatsServiceId = new("48eb4627-8692-4a3b-8be1-be64bbeea534");
        public static readonly Guid PartnerCouponId = new("245e1c8a-0208-4016-acd1-a36f45315e88");

        public const int DiscountPerConnection = 15;
        public const string Name = "Fiber + Phone Bundle";
        public const string Description = "$15/mo off fiber internet when bundled with phone service.";
        public const string PartnerDescription = "$15/mo off fiber internet at partner pricing.";

        /// <summary>
        /// The contract terms fiber internet can be sold on, in years.
        /// </summary>
        public static readonly int[] TermYears = [2, 3, 5];

        public static bool IsFiberInternet(Guid serviceId) => serviceId == FiberInternet300ServiceId || serviceId == FiberInternet1GServiceId;

        public static bool IsPhoneService(Guid serviceId) => serviceId == StandardLinesServiceId || serviceId == ConcurrentSeatsServiceId;

        /// <summary>
        /// The parts of a product order the bundle depends on, so the Ops site's own ProductOrder model can use the same rules.
        /// </summary>
        public readonly record struct Line(Guid ServiceId, long Quantity, Guid? CouponId);

        public static Line[] ToLines(IEnumerable<ProductOrder> productOrders) => [.. productOrders.Select(x => new Line(x.ServiceId, x.Quantity, x.CouponId))];

        /// <summary>
        /// The number of fiber internet connections in the cart.
        /// </summary>
        public static int FiberConnections(IEnumerable<ProductOrder> productOrders) => FiberConnections(ToLines(productOrders));

        public static int FiberConnections(IEnumerable<Line> lines) =>
            (int)lines.Where(x => IsFiberInternet(x.ServiceId)).Sum(x => Math.Max(x.Quantity, 0));

        /// <summary>
        /// Whether the bundle discount comes from the Partner coupon rather than phone service.
        /// </summary>
        public static bool IsPartnerOnly(IEnumerable<ProductOrder> productOrders) => IsPartnerOnly(ToLines(productOrders));

        public static bool IsPartnerOnly(IEnumerable<Line> lines) => !HasPhoneService(lines) && HasPartnerCoupon(lines);

        /// <summary>
        /// The total monthly bundle discount in dollars, as a positive number. Applied once per fiber connection no matter how many phone services or coupons qualify it.
        /// </summary>
        public static int Discount(IEnumerable<ProductOrder> productOrders) => Discount(ToLines(productOrders));

        public static int Discount(IEnumerable<Line> lines)
        {
            var all = lines as ICollection<Line> ?? [.. lines];
            return HasPhoneService(all) || HasPartnerCoupon(all) ? FiberConnections(all) * DiscountPerConnection : 0;
        }

        private static bool HasPhoneService(IEnumerable<Line> lines) => lines.Any(x => IsPhoneService(x.ServiceId) && x.Quantity > 0);

        private static bool HasPartnerCoupon(IEnumerable<Line> lines) => lines.Any(x => x.CouponId == PartnerCouponId);
    }
}
