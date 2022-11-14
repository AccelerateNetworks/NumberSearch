using NumberSearch.DataAccess;

using System.Collections.Generic;

namespace NumberSearch.Mvc
{
    public class IngestResults
    {
        public IEnumerable<IngestStatistics>? Ingests { get; set; }
        public IEnumerable<PhoneNumber.CountByProvider>? CurrentState { get; set; }
        public IEnumerable<PhoneNumber.CountNPA>? AreaCodes { get; set; }
        public int TotalPhoneNumbers { get; set; }
        public int TotalExecutiveNumbers { get; set; }
        public int TotalPremiumNumbers { get; set; }
        public int TotalTollFreeNumbers { get; set; }
        public int TotalStandardNumbers { get; set; }
    }
}