using Microsoft.Build.Framework;

using System;
using System.Collections.Generic;

namespace AccelerateNetworks.Operations
{
    public partial class Carrier
    {
        public Guid CarrierId { get; set; }
        public string? Ocn { get; set; }
        public string? Lec { get; set; }
        public string? Lectype { get; set; }
        public string? Spid { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Ratecenter { get; set; }
        public string? Color { get; set; }
        public string? LogoLink { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
