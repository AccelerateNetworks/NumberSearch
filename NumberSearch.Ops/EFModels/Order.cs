using System;
using System.Collections.Generic;

#nullable disable

namespace NumberSearch.Ops.EFModels
{
    public partial class Order
    {
        public Guid OrderId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public DateTime DateSubmitted { get; set; }
        public string BusinessName { get; set; }
        public string CustomerNotes { get; set; }
        public string BillingClientId { get; set; }
        public string BillingInvoiceId { get; set; }
        public bool? Quote { get; set; }
        public string BillingInvoiceReoccuringId { get; set; }
        public string SalesEmail { get; set; }
        public bool BackgroundWorkCompleted { get; set; }
        public bool Completed { get; set; }
        public DateTime? InstallDate { get; set; }
        public string UpfrontInvoiceLink { get; set; }
        public string ReoccuringInvoiceLink { get; set; }
        public bool OnsiteInstallation { get; set; }
        public string AddressUnitType { get; set; }
        public string AddressUnitNumber { get; set; }
        public string UnparsedAddress { get; set; }
    }
}
