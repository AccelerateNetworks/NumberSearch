using System;

namespace AccelerateNetworks.Operations
{
    public partial class PhoneMenuOption
    {
        public Guid PhoneMenuOptionId { get; set; }
        public Guid NewClientId { get; set; }
        public string MenuOption { get; set; } = null!;
        public string Destination { get; set; } = null!;
        public string Description { get; set; } = null!;
        public DateTime? DateUpdated { get; set; }
    }
}
