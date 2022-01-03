using System;
using System.Collections.Generic;

namespace AccelerateNetworks.Operations
{
    public partial class SpeedDialKey
    {
        public Guid SpeedDialKeyId { get; set; }
        public Guid NewClientId { get; set; }
        public string? NumberOrExtension { get; set; }
        public string? LabelOrName { get; set; }
        public DateTime? DateUpdated { get; set; }
    }
}
