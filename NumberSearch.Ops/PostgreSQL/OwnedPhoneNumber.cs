﻿using System;
using System.Collections.Generic;

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
        public string? Lidbcnam { get; set; }
        public Guid? EmergencyInformationId { get; set; }
    }
}