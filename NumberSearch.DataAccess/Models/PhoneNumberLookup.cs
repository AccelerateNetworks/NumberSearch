using Dapper;

using Npgsql;

using NumberSearch.DataAccess.BulkVS;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class PhoneNumberLookup
    {
        public Guid PhoneNumberLookupId { get; set; }
        public string DialedNumber { get; set; }
        public string LRN { get; set; }
        public string OCN { get; set; }
        public string LATA { get; set; }
        public string City { get; set; }
        public string Ratecenter { get; set; }
        public string State { get; set; }
        public string Jurisdiction { get; set; }
        public bool Local { get; set; }
        public string LEC { get; set; }
        public string LECType { get; set; }
        public string SPID { get; set; }
        public string LIDBName { get; set; }
        public DateTime LastPorted { get; set; }
        public string IngestedFrom { get; set; }
        public DateTime DateIngested { get; set; }

        public PhoneNumberLookup(LrnBulkCnam source)
        {
            PhoneNumberLookupId = Guid.NewGuid();
            DialedNumber = source.tn;
            LRN = source.lrn;
            OCN = source.ocn;
            LATA = source.lata;
            City = source.city;
            Ratecenter = source.ratecenter;
            State = source.province;
            Jurisdiction = source.jurisdiction;
            Local = source.local == "Y" ? true : false;
            LEC = source.lec;
            LECType = source.lectype;
            SPID = source.spid;
            LIDBName = source.LIDBName;
            LastPorted = source.LastPorted;
            IngestedFrom = "BulkVS";
            DateIngested = DateTime.Now;
        }

        /// <summary>
        /// Get all of the phone number lookups in the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PhoneNumberLookup>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryAsync<PhoneNumberLookup>("SELECT \"PhoneNumberLookupId\", \"DialedNumber\", \"LRN\", \"OCN\", \"LATA\", \"City\", \"Ratecenter\", \"State\", \"Jurisdiction\", \"Local\", \"LEC\", \"LECType\", \"SPID\", \"LIDBName\", \"LastPorted\", \"IngestedFrom\", \"DateIngested\" " +
                "FROM public.\"PhoneNumberLookups\"")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Added new number lookups to the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PostAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"PhoneNumberLookups\" ( \"DialedNumber\", \"LRN\", \"OCN\", \"LATA\", \"City\", \"Ratecenter\", \"State\", \"Jurisdiction\", \"Local\", \"LEC\", \"LECType\", \"SPID\", \"LIDBName\", \"LastPorted\", \"IngestedFrom\", \"DateIngested\") " +
                "VALUES ( @DialedNumber, @LRN, @OCN, @LATA, @City, @Ratecenter, @State, @Jurisdiction, @Local, @LEC, @LECType, @SPID, @LIDBName, @LastPorted, @IngestedFrom, @DateIngested )",
                new { DialedNumber, LRN, OCN, LATA, City, Ratecenter, State, Jurisdiction, Local, LEC, LECType, SPID, LIDBName, LastPorted, IngestedFrom, DateIngested })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
