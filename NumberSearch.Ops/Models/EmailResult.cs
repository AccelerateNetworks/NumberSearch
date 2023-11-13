using AccelerateNetworks.Operations;

using System;

namespace NumberSearch.Ops
{
    public class EmailResult
    {
        public SentEmail[] Emails { get; set; } = Array.Empty<SentEmail>();
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
    }
}