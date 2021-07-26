using System;
using System.Collections.Generic;

#nullable disable

namespace NumberSearch.Ops.EFModels
{
    public partial class PortedPhoneNumber
    {
        public string PortedDialedNumber { get; set; }
        public int Npa { get; set; }
        public int Nxx { get; set; }
        public int Xxxx { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string IngestedFrom { get; set; }
        public DateTime DateIngested { get; set; }
        public Guid? PortRequestId { get; set; }
        public Guid? OrderId { get; set; }
        public bool? Wireless { get; set; }
        public string RequestStatus { get; set; }
        public DateTime? DateFirmOrderCommitment { get; set; }
        public Guid PortedPhoneNumberId { get; set; }
        public string ExternalPortRequestId { get; set; }
        public bool Completed { get; set; }
        public string RawResponse { get; set; }
    }
}
