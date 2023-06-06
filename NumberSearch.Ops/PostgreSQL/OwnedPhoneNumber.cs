using System;

namespace AccelerateNetworks.Operations
{
    public partial class OwnedPhoneNumber
    {
        public Guid OwnedPhoneNumberId { get; set; }
        public string DialedNumber { get; set; } = null!;
        public string IngestedFrom { get; set; } = null!;
        public DateTime DateIngested { get; set; }
        public bool Active { get; set; }
        public string? BillingClientId { get; set; }
        public string? OwnedBy { get; set; }
        public string? Notes { get; set; }
        public string? SPID { get; set; }
        public string? SPIDName { get; set; }
        public string? LIDBCNAM { get; set; }
        public Guid? EmergencyInformationId { get; set; }
        public DateTime DateUpdated { get; set; }
        public string? Status { get; set; }
        public string? FusionPBXClientId { get; set; }
        public Guid? FPBXDomainId { get; set; } = null;
        public Guid? FPBXDestinationId { get; set; } = null;
        public string? FPBXDomainName { get; set; } = string.Empty;
        public string? FPBXDomainDescription { get; set; } = string.Empty;
        public string? SMSRoute { get; set; } = string.Empty;
    }
}
