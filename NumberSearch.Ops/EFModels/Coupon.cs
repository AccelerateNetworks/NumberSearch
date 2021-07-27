using System;
using System.Collections.Generic;

#nullable disable

namespace NumberSearch.Ops.EFModels
{
    public partial class Coupon
    {
        public Guid CouponId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Public { get; set; }
    }
}
