using System.ComponentModel.DataAnnotations;

namespace NumberSearch.Ops.Models
{
    public class ToEmailRequest
    {
        public string DialedNumber { get; set; } = string.Empty;
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}