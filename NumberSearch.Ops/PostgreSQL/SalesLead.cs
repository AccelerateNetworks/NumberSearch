using System;

namespace AccelerateNetworks.Operations
{
    public partial class SalesLead
    {
        public Guid Id { get; set; }
        public string? BusinessName { get; set; }
        public string? RoleTitle { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string PhoneNumber { get; set; } = null!;
        public DateTime DateSubmitted { get; set; }
    }
}
