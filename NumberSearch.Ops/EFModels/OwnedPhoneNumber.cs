using System;
using System.Collections.Generic;

#nullable disable

namespace NumberSearch.Ops.EFModels
{
    public partial class OwnedPhoneNumber
    {
        public Guid OwnedPhoneNumberId { get; set; }
        public string DialedNumber { get; set; }
        public string IngestedFrom { get; set; }
        public DateTime DateIngested { get; set; }
        public bool Active { get; set; }
        public string BillingClientId { get; set; }
        public string OwnedBy { get; set; }
        public string Notes { get; set; }
        public string Spid { get; set; }
        public string Spidname { get; set; }
        public string Lidbcnam { get; set; }
        public Guid? EmergencyInformationId { get; set; }
    }
}
