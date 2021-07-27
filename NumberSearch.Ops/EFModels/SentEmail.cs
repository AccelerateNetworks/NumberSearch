using System;
using System.Collections.Generic;

#nullable disable

namespace NumberSearch.Ops.EFModels
{
    public partial class SentEmail
    {
        public Guid EmailId { get; set; }
        public Guid OrderId { get; set; }
        public string PrimaryEmailAddress { get; set; }
        public string CarbonCopy { get; set; }
        public string Subject { get; set; }
        public string MessageBody { get; set; }
        public DateTime DateSent { get; set; }
        public bool Completed { get; set; }
        public string SalesEmailAddress { get; set; }
        public string CalendarInvite { get; set; }
    }
}
