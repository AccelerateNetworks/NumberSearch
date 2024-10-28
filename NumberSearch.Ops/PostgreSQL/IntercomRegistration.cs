using System;

namespace AccelerateNetworks.Operations
{
    public partial class IntercomRegistration
    {
        public Guid IntercomRegistrationId { get; set; }
        public Guid NewClientId { get; set; }
        public int? ExtensionSendingIntercom { get; set; }
        public int? ExtensionRecievingIntercom { get; set; }
        public DateTime? DateUpdated { get; set; }
    }
}
