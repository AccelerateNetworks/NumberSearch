using NumberSearch.DataAccess;

using System.Collections.Generic;

namespace NumberSearch.Mvc
{
    public class IngestResults
    {
        public IEnumerable<IngestStatistics>? Ingests { get; set; }
        public IEnumerable<(string, int)>? CurrentState { get; set; }
        public IEnumerable<(int, int)>? PriorityAreaCodes { get; set; }

        public int TotalPhoneNumbers { get; set; }
        public int TotalExecutiveNumbers { get; set; }
        public int TotalPremiumNumbers { get; set; }
        public int TotalStandardNumbers { get; set; }
    }
}