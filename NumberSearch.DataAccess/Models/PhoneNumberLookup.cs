﻿using Dapper;

using Npgsql;

using NumberSearch.DataAccess.BulkVS;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class PhoneNumberLookup
    {
        public Guid PhoneNumberLookupId { get; set; } = new();
        public string DialedNumber { get; set; } = string.Empty;
        public string LRN { get; set; } = string.Empty;
        public string OCN { get; set; } = string.Empty;
        public string LATA { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Ratecenter { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Jurisdiction { get; set; } = string.Empty;
        public bool Local { get; set; }
        public string LEC { get; set; } = string.Empty;
        public string LECType { get; set; } = string.Empty;
        public string SPID { get; set; } = string.Empty;
        public string LIDBName { get; set; } = string.Empty;
        public string LosingCarrier { get; set; } = string.Empty;
        public DateTime LastPorted { get; set; }
        public string IngestedFrom { get; set; } = string.Empty;
        public DateTime DateIngested { get; set; }
        public Guid? CarrierId { get; set; }

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
            Local = source.local == "Y";
            LEC = source.lec;
            LECType = source.lectype;
            SPID = source.spid;
            LIDBName = source.LIDBName;
            LastPorted = source.LastPorted;
            IngestedFrom = "BulkVS";
            DateIngested = DateTime.Now;
        }

        public PhoneNumberLookup()
        {
        }

        /// <summary>
        /// Get all of the phone number lookups in the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PhoneNumberLookup>> GetAllAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryAsync<PhoneNumberLookup>("SELECT \"PhoneNumberLookupId\", \"DialedNumber\", \"LRN\", \"OCN\", \"LATA\", \"City\", \"Ratecenter\", \"State\", \"Jurisdiction\", \"Local\", \"LEC\", \"LECType\", \"SPID\", \"LIDBName\", \"LastPorted\", \"IngestedFrom\", \"DateIngested\", \"CarrierId\" " +
                "FROM public.\"PhoneNumberLookups\"")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Get a specific phone number lookup from the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<PhoneNumberLookup?> GetByDialedNumberAsync(string dialedNumber, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryFirstOrDefaultAsync<PhoneNumberLookup>("SELECT \"PhoneNumberLookupId\", \"DialedNumber\", \"LRN\", \"OCN\", \"LATA\", \"City\", \"Ratecenter\", \"State\", \"Jurisdiction\", \"Local\", \"LEC\", \"LECType\", \"SPID\", \"LIDBName\", \"LastPorted\", \"IngestedFrom\", \"DateIngested\", \"CarrierId\" " +
                "FROM public.\"PhoneNumberLookups\" WHERE \"DialedNumber\" = @DialedNumber", new { DialedNumber = dialedNumber })
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Added new number lookups to the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"PhoneNumberLookups\" ( \"DialedNumber\", \"LRN\", \"OCN\", \"LATA\", \"City\", \"Ratecenter\", \"State\", \"Jurisdiction\", \"Local\", \"LEC\", \"LECType\", \"SPID\", \"LIDBName\", \"LastPorted\", \"IngestedFrom\", \"DateIngested\", \"CarrierId\") " +
                "VALUES ( @DialedNumber, @LRN, @OCN, @LATA, @City, @Ratecenter, @State, @Jurisdiction, @Local, @LEC, @LECType, @SPID, @LIDBName, @LastPorted, @IngestedFrom, @DateIngested, @CarrierId )",
                new { DialedNumber, LRN, OCN, LATA, City, Ratecenter, State, Jurisdiction, Local, LEC, LECType, SPID, LIDBName, LastPorted, IngestedFrom, DateIngested, CarrierId })
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
