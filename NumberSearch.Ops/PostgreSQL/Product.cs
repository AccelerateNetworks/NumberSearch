﻿using System;

namespace AccelerateNetworks.Operations
{
    public partial class Product
    {
        public Guid ProductId { get; set; }
        public string? Name { get; set; }
        public string? Price { get; set; }
        public string? Description { get; set; }
        public string? Image { get; set; }
        public bool Public { get; set; }
        public int QuantityAvailable { get; set; }
        public string? SupportLink { get; set; }
        public int? DisplayPriority { get; set; }
        public string? VendorPartNumber { get; set; }
        public string? Type { get; set; }
        public string? Tags { get; set; }
        public string? VendorDescription { get; set; }
        public string? VendorFeatures { get; set; }
        public string? MarkdownContent { get; set; }
        public decimal InstallTime { get; set; }
    }
}
