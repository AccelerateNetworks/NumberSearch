using System;

namespace AccelerateNetworks.Operations
{
    public partial class ExtensionRegistration
    {
        public Guid ExtensionRegistrationId { get; set; }
        public Guid NewClientId { get; set; }
        public int? ExtensionNumber { get; set; }
        public string? NameOrLocation { get; set; }
        public string? Email { get; set; }
        public string? ModelOfPhone { get; set; }
        public string? OutboundCallerId { get; set; }
        public DateTime? DateUpdated { get; set; }
    }
}
