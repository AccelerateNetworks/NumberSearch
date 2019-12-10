namespace NumberSearch.Ingest
{
    public class IngestStatistics
    {
        public int NumbersRetrived { get; set; }
        public int IngestedNew { get; set; }
        public int FailedToIngest { get; set; }
        public int UpdatedExisting { get; set; }
        public int Unchanged { get; set; }
    }
}
