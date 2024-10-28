using NumberSearch.DataAccess;

namespace NumberSearch.Mvc
{
    public class IngestResults
    {
        public IngestStatistics[] Ingests { get; set; } = [];
        public PhoneNumber.CountByProvider[] CurrentState { get; set; } = [];
        public PhoneNumber.CountNPA[] AreaCodes { get; set; } = [];
        public PhoneNumber.CountNPA[] PriorityAreaCodes { get; set; } = [];
        public int TotalPhoneNumbers { get; set; }
        public int TotalExecutiveNumbers { get; set; }
        public int TotalPremiumNumbers { get; set; }
        public int TotalTollFreeNumbers { get; set; }
        public int TotalStandardNumbers { get; set; }
    }
}