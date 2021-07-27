using System;
using System.Collections.Generic;
using System.Text;

namespace NumberSearch.DataAccess
{
    public class NewClient
    {
        public Guid NewClientId { get; set; }
        public Guid OrderId { get; set; }
        public string BillingClientId { get; set; }
        public ExtensionRegistration[] ExtensionRegistrations { get; set; }
        public bool PhoneMenu { get; set; }
        public string PhonesToRingOrMenuDescription { get; set; }
        public string BusinessHours { get; set; }
        public string AfterHoursVoicemail { get; set; }
        public NumberDescription[] NumberDescriptions { get; set; }
        public bool TextingService { get; set; }
        public string TextingServiceName { get; set; }
        public bool OverheadPaging { get; set; }
        public string OverheadPagingDescription { get; set; }
        public bool Intercom { get; set; }
        public IntercomRegistration[] IntercomRegistrations { get; set; }
        public SpeedDialKey[] SpeedDialKeys { get; set; }
        public bool CustomHoldMusic { get; set; }
        public string HoldMusicDescription { get; set; }
        public string PhoneOfflineInstructions { get; set; }
        public FollowMeRegistration[] FollowMeRegistrations { get; set; }
    }

    public class ExtensionRegistration
    {
        public Guid ExtensionRegistrationId { get; set; }
        public Guid NewClientId { get; set; }
        public int ExtensionNumber { get; set; }
        public string NameOrLocation { get; set; }
        public string Email { get; set; }
        public string ModelOfPhone { get; set; }
        public string OutboundCallerId { get; set; }
    }

    public class NumberDescription
    {
        public Guid NumberDescriptionId { get; set; }
        public Guid NewClientId { get; set; }
        public string PhoneNumber { get; set; }
        public string Description { get; set; }
        public string Prefix { get; set; }
    }

    public class IntercomRegistration
    {
        public Guid IntercomRegistrationId { get; set; }
        public Guid NewClientId { get; set; }
        public int ExtensionSendingIntercom { get; set; }
        public int ExtensionRecievingIntercom { get; set; }
    }

    public class SpeedDialKey
    {
        public Guid SpeedDialKeyId { get; set; }
        public Guid NewClientId { get; set; }
        public string NumberOrExtension { get; set; }
        public string LabelOrName { get; set; }
    }

    public class FollowMeRegistration
    {
        public Guid FollowMeRegistrationId { get; set; }
        public Guid NewClientId { get; set; }
        public string ExtensionOrNumber { get; set; }
        public string CellPhoneNumber { get; set; }
        public string UnreachablePhoneNumber { get; set; }
    }
}
