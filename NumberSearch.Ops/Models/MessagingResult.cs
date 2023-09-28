using AccelerateNetworks.Operations;

using Models;

using System;

namespace NumberSearch.Ops.Models
{
    public class MessagingResult
    {
        public ClientRegistration[] ClientRegistrations { get; set; } = Array.Empty<ClientRegistration>();
        public MessageRecord[] FailedMessages { get; set; } = Array.Empty<MessageRecord>();
        public OwnedPhoneNumber[] Owned { get; set; } = Array.Empty<OwnedPhoneNumber>();
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public RegistrationRequest RegistrationRequest { get; set; } = new();
        public ToEmailRequest ToEmail { get; set; } = new();
    }
}
