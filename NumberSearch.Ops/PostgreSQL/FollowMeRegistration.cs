using System;

namespace AccelerateNetworks.Operations
{
    public partial class FollowMeRegistration
    {
        public Guid FollowMeRegistrationId { get; set; }
        public Guid NewClientId { get; set; }
        public string? NumberOrExtension { get; set; }
        public string? CellPhoneNumber { get; set; }
        public string? UnreachablePhoneNumber { get; set; }
        public DateTime? DateUpdated { get; set; }
    }
}
