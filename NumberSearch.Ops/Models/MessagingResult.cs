using AccelerateNetworks.Operations;

using Models;

using System;
using System.Collections.Generic;

namespace NumberSearch.Ops.Models
{
    public class MessagingResult
    {
        public UsageSummary[] UsageSummary { get; set; } = Array.Empty<UsageSummary>();
        public MessageRecord[] FailedMessages { get; set; } = Array.Empty<MessageRecord>();
        public OwnedPhoneNumber[] Owned { get; set; } = Array.Empty<OwnedPhoneNumber>();
    }
}
