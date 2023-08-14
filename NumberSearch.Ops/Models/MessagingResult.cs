using Models;

using System;

namespace NumberSearch.Ops.Models
{
    public class MessagingResult
    {
        public UsageSummary[] UsageSummary { get; set; } = Array.Empty<UsageSummary>();
        public MessageRecord[] FailedMessages { get; set; } = Array.Empty<MessageRecord>();
    }
}
