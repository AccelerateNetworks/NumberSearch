using System;
using System.Collections.Generic;

#nullable disable

namespace NumberSearch.Ops.EFModels
{
    public partial class Service
    {
        public Guid ServiceId { get; set; }
        public string Name { get; set; }
        public string Price { get; set; }
        public string Description { get; set; }
    }
}
