using System;

namespace AccelerateNetworks.Operations
{
    public partial class Ingest1
    {
        public Guid Id { get; set; }
        public long NumbersRetrived { get; set; }
        public long IngestedNew { get; set; }
        public long FailedToIngest { get; set; }
        public long UpdatedExisting { get; set; }
        public long Unchanged { get; set; }
        public long Removed { get; set; }
        public string? IngestedFrom { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool? Lock { get; set; }
        public bool? Priority { get; set; }
    }
}
