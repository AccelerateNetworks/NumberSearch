using System;
using System.Collections.Generic;

namespace AccelerateNetworks.Operations
{
    public partial class PortRequest
    {
        public Guid PortRequestId { get; set; }
        public Guid OrderId { get; set; }
        public string? Address { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? BillingPhone { get; set; }
        public string? LocationType { get; set; }
        public string? BusinessContact { get; set; }
        public string? BusinessName { get; set; }
        public string? ProviderAccountNumber { get; set; }
        public string? ProviderPIN { get; set; }
        public bool PartialPort { get; set; }
        public string? PartialPortDescription { get; set; }
        public bool WirelessNumber { get; set; }
        public string? CallerId { get; set; }
        public string? BillImagePath { get; set; }
        public string? BillImageFileType { get; set; }
        public DateTime DateSubmitted { get; set; }
        public string? ResidentialFirstName { get; set; }
        public string? ResidentialLastName { get; set; }
        public string? TeliId { get; set; }
        public string? RequestStatus { get; set; }
        public bool Completed { get; set; }
        public DateTime? DateCompleted { get; set; }
        public DateTime? DateUpdated { get; set; }
        public string? VendorSubmittedTo { get; set; }
        public DateTime? TargetDate { get; set; }
    }
}
