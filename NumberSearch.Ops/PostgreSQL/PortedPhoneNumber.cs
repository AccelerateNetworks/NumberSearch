using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccelerateNetworks.Operations;

public partial class PortedPhoneNumber
{
    public string PortedDialedNumber { get; set; } = null!;
    public int NPA { get; set; }
    public int NXX { get; set; }
    public int XXXX { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string IngestedFrom { get; set; } = null!;
    public DateTime DateIngested { get; set; }
    public Guid? PortRequestId { get; set; }
    public Guid? OrderId { get; set; }
    public bool? Wireless { get; set; }
    public string? RequestStatus { get; set; }
    public DateTime? DateFirmOrderCommitment { get; set; }
    public Guid PortedPhoneNumberId { get; set; }
    public string? ExternalPortRequestId { get; set; }
    public bool Completed { get; set; }
    public string? RawResponse { get; set; }
    [NotMapped]
    public bool Portable { get; set; }
    [NotMapped]
    public PhoneNumberLookup? LrnLookup { get; set; }
}