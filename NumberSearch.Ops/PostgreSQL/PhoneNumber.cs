using System;

namespace AccelerateNetworks.Operations;

public partial class PhoneNumber
{
    public string DialedNumber { get; set; } = null!;
    public int NPA { get; set; }
    public int NXX { get; set; }
    public int XXXX { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string IngestedFrom { get; set; } = null!;
    public DateTime DateIngested { get; set; }
    public string? NumberType { get; set; }
    public bool Purchased { get; set; }
}