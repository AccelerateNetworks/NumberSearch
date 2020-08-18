using NumberSearch.DataAccess;

using System.Collections.Generic;

namespace NumberSearch.Mvc
{
    public class IngestResults
    {
        public IEnumerable<IngestStatistics> Ingests { get; set; }
        public IEnumerable<(string, int)> CurrentState { get; set; }

        public int TotalPhoneNumbers { get; set; }
    }
}