using System;

namespace AccelerateNetworks.Operations
{
    public partial class VerifiedPhoneNumber
    {
        public Guid VerifiedPhoneNumberId { get; set; }
        public string VerifiedDialedNumber { get; set; } = null!;
        public int NPA { get; set; }
        public int NXX { get; set; }
        public int XXXX { get; set; }
        public string IngestedFrom { get; set; } = null!;
        public DateTime DateIngested { get; set; }
        public Guid OrderId { get; set; }
        public bool Wireless { get; set; }
        public string? NumberType { get; set; }
        public string? LocalRoutingNumber { get; set; }
        public string? OperatingCompanyNumber { get; set; }
        public string? City { get; set; }
        public string? LocalAccessTransportArea { get; set; }
        public string? RateCenter { get; set; }
        public string? Province { get; set; }
        public string? Jurisdiction { get; set; }
        public string? Local { get; set; }
        public string? LocalExchangeCarrier { get; set; }
        public string? LocalExchangeCarrierType { get; set; }
        public string? ServiceProfileIdentifier { get; set; }
        public string? Activation { get; set; }
        public string? LIDBName { get; set; }
        public DateTime LastPorted { get; set; }
        public DateTime? DateToExpire { get; set; }
    }
}
