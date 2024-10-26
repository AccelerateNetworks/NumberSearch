using AccelerateNetworks.Operations;

namespace NumberSearch.Ops.Models
{
    public class EmailResult
    {
        public SentEmail[] Emails { get; set; } = [];
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
    }
}