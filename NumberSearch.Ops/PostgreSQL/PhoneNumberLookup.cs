using System;
using System.Collections.Generic;

namespace AccelerateNetworks.Operations
{
    public partial class PhoneNumberLookup
    {
        public Guid PhoneNumberLookupId { get; set; }
        public string DialedNumber { get; set; } = null!;
        public string? Lrn { get; set; }
        public string? Ocn { get; set; }
        public string? Lata { get; set; }
        public string? City { get; set; }
        public string? Ratecenter { get; set; }
        public string? State { get; set; }
        public string? Jurisdiction { get; set; }
        public bool Local { get; set; }
        public string? Lec { get; set; }
        public string? Lectype { get; set; }
        public string? Spid { get; set; }
        public string? Lidbname { get; set; }
        public DateTime? LastPorted { get; set; }
        public string? IngestedFrom { get; set; }
        public DateTime DateIngested { get; set; }
        public Guid? CarrierId { get; set; }
    }
}
