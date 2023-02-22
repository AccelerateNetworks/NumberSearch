using NumberSearch.DataAccess;

using System;

namespace NumberSearch.Mvc
{
    public class IngestResults
    {
        public IngestStatistics[] Ingests { get; set; } = Array.Empty<IngestStatistics>();
        public PhoneNumber.CountByProvider[] CurrentState { get; set; } = Array.Empty<PhoneNumber.CountByProvider>();
        public PhoneNumber.CountNPA[] AreaCodes { get; set; } = Array.Empty<PhoneNumber.CountNPA>();
        public int TotalPhoneNumbers { get; set; }
        public int TotalExecutiveNumbers { get; set; }
        public int TotalPremiumNumbers { get; set; }
        public int TotalTollFreeNumbers { get; set; }
        public int TotalStandardNumbers { get; set; }
    }
}