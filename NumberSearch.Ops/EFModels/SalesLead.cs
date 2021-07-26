using System;
using System.Collections.Generic;

#nullable disable

namespace NumberSearch.Ops.EFModels
{
    public partial class SalesLead
    {
        public Guid Id { get; set; }
        public string BusinessName { get; set; }
        public string RoleTitle { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateSubmitted { get; set; }
    }
}
