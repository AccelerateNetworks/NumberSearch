﻿using System;
using System.ComponentModel.DataAnnotations;

namespace AccelerateNetworks.Operations
{
    public partial class SentEmail
    {
        [Key]
        public Guid EmailId { get; set; }
        public Guid OrderId { get; set; }
        public string PrimaryEmailAddress { get; set; } = null!;
        public string CarbonCopy { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string MessageBody { get; set; } = null!;
        public DateTime DateSent { get; set; }
        public bool Completed { get; set; }
        public string? SalesEmailAddress { get; set; }
        public string? CalendarInvite { get; set; }
        public bool DoNotSend { get; set; }
    }
}
