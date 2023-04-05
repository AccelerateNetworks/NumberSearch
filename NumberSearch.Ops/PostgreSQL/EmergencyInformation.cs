using System;
using System.Collections.Generic;
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
        public string? TeliId { get; set; }
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? UnitType { get; set; }
        public string? UnitNumber { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string? AlertGroup { get; set; }
        public string? Note { get; set; }
    }
}
