using AccelerateNetworks.Operations;

using Models;

namespace NumberSearch.Ops.Models
{
    public class MessagingResult
    {
        public ClientRegistration[] ClientRegistrations { get; set; } = [];
        public MessageRecord[] FailedMessages { get; set; } = [];
        public OwnedPhoneNumber[] Owned { get; set; } = [];
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public RegistrationRequest RegistrationRequest { get; set; } = new();
        public ToEmailRequest ToEmail { get; set; } = new();
        public string CarrierName { get; set; } = string.Empty;
    }
}
