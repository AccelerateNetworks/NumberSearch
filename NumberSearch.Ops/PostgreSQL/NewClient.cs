using System;

namespace AccelerateNetworks.Operations
{
    public partial class NewClient
    {
        public Guid NewClientId { get; set; }
        public Guid OrderId { get; set; }
        public string? BillingClientId { get; set; }
        public bool PhoneMenu { get; set; }
        public string? PhonesToRingOrMenuDescription { get; set; }
        public string? BusinessHours { get; set; }
        public string? AfterHoursVoicemail { get; set; }
        public bool TextingService { get; set; }
        public string? TextingServiceName { get; set; }
        public bool OverheadPaging { get; set; }
        public string? OverheadPagingDescription { get; set; }
        public bool Intercom { get; set; }
        public bool CustomHoldMusic { get; set; }
        public string? HoldMusicDescription { get; set; }
        public string? PhoneOfflineInstructions { get; set; }
        public DateTime? DateUpdated { get; set; }
        public bool SpeedDial { get; set; }
        public string? IntercomDescription { get; set; }
    }
}
