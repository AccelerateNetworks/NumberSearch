﻿using System;
using System.Collections.Generic;

#nullable disable

namespace NumberSearch.Ops.EFModels
{
    public partial class IngestCycle
    {
        public Guid IngestCycleId { get; set; }
        public string IngestedFrom { get; set; }
        public TimeSpan CycleTime { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool? Enabled { get; set; }
        public bool? RunNow { get; set; }
    }
}