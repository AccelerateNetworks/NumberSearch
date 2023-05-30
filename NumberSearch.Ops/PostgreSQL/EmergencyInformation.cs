using System;
using System.ComponentModel.DataAnnotations;

namespace AccelerateNetworks.Operations
{
    public partial class EmergencyInformation
    {
        [Key]
        public Guid EmergencyInformationId { get; set; }
        public string DialedNumber { get; set; } = null!;
        public string IngestedFrom { get; set; } = null!;
        public DateTime DateIngested { get; set; }
        public string? CallerName { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? Sms { get; set; }
        public string? RawResponse { get; set; }
        public DateTime? BulkVSLastModificationDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
