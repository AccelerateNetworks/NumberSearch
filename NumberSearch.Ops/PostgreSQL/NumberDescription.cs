using System;

namespace AccelerateNetworks.Operations
{
    public partial class NumberDescription
    {
        public Guid NumberDescriptionId { get; set; }
        public Guid NewClientId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Description { get; set; }
        public string? Prefix { get; set; }
        public DateTime? DateUpdated { get; set; }
    }
}
