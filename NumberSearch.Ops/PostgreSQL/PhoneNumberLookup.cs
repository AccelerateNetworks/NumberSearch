using NumberSearch.DataAccess.BulkVS;

using System;

namespace AccelerateNetworks.Operations
{
    public partial class PhoneNumberLookup
    {
        public Guid PhoneNumberLookupId { get; set; }
        public string DialedNumber { get; set; } = null!;
        public string? Lrn { get; set; }
        public string? Ocn { get; set; }
        public string? Lata { get; set; }
        public string? City { get; set; }
        public string? Ratecenter { get; set; }
        public string? State { get; set; }
        public string? Jurisdiction { get; set; }
        public bool Local { get; set; }
        public string? Lec { get; set; }
        public string? Lectype { get; set; }
        public string? Spid { get; set; }
        public string? Lidbname { get; set; }
        public DateTime? LastPorted { get; set; }
        public string? IngestedFrom { get; set; }
        public DateTime DateIngested { get; set; }
        public Guid? CarrierId { get; set; }
        public PhoneNumberLookup(LrnBulkCnam source)
        {
            PhoneNumberLookupId = Guid.NewGuid();
            DialedNumber = source.tn;
            Lrn = source.lrn;
            Ocn = source.ocn;
            Lata = source.lata;
            City = source.city;
            Ratecenter = source.ratecenter;
            State = source.province;
            Jurisdiction = source.jurisdiction;
            Local = source.local == "Y";
            Lec = source.lec;
            Lectype = source.lectype;
            Spid = source.spid;
            Lidbname = source.LIDBName;
            LastPorted = source.LastPorted;
            IngestedFrom = "BulkVS";
            DateIngested = DateTime.Now;
        }
        public PhoneNumberLookup()
        {

        }
    }
}
