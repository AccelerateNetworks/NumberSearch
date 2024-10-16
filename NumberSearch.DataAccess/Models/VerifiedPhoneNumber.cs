﻿using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class VerifiedPhoneNumber
    {
        public Guid VerifiedPhoneNumberId { get; set; }
        public string VerifiedDialedNumber { get; set; } = string.Empty;
        public int NPA { get; set; }
        public int NXX { get; set; }
        public int XXXX { get; set; }
        public string City { get; set; } = string.Empty;
        public string IngestedFrom { get; set; } = string.Empty;
        public DateTime DateIngested { get; set; }
        public Guid? OrderId { get; set; }
        public bool Wireless { get; set; }
        public string NumberType { get; set; } = string.Empty;
        public string LocalRoutingNumber { get; set; } = string.Empty;
        public string OperatingCompanyNumber { get; set; } = string.Empty;
        public string LocalAccessTransportArea { get; set; } = string.Empty;
        public string RateCenter { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Jurisdiction { get; set; } = string.Empty;
        public string Local { get; set; } = string.Empty;
        public string LocalExchangeCarrier { get; set; } = string.Empty;
        public string LocalExchangeCarrierType { get; set; } = string.Empty;
        public string ServiceProfileIdentifier { get; set; } = string.Empty;
        public string Activation { get; set; } = string.Empty;
        public string LIDBName { get; set; } = string.Empty;
        public DateTime LastPorted { get; set; }
        public DateTime DateToExpire { get; set; }

        /// <summary>
        /// Get a list of all verified phone numbers in the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<VerifiedPhoneNumber>> GetAllAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<VerifiedPhoneNumber>("SELECT \"VerifiedPhoneNumberId\", \"VerifiedDialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"IngestedFrom\", \"DateIngested\", \"OrderId\", \"Wireless\", \"NumberType\", \"LocalRoutingNumber\", \"OperatingCompanyNumber\", \"City\", \"LocalAccessTransportArea\", \"RateCenter\", \"Province\", \"Jurisdiction\", \"Local\", \"LocalExchangeCarrier\", \"LocalExchangeCarrierType\", \"ServiceProfileIdentifier\", \"Activation\", \"LIDBName\", \"LastPorted\", \"DateToExpire\" FROM public.\"VerifiedPhoneNumbers\"")
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Find a single phone number based on the complete number.
        /// </summary>
        /// <param name="dialedNumber"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<VerifiedPhoneNumber>> GetByOrderIdAsync(Guid orderId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<VerifiedPhoneNumber>("SELECT \"VerifiedPhoneNumberId\", \"VerifiedDialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"IngestedFrom\", \"DateIngested\", \"OrderId\", \"Wireless\", \"NumberType\", \"LocalRoutingNumber\", \"OperatingCompanyNumber\", \"City\", \"LocalAccessTransportArea\", \"RateCenter\", \"Province\", \"Jurisdiction\", \"Local\", \"LocalExchangeCarrier\", \"LocalExchangeCarrierType\", \"ServiceProfileIdentifier\", \"Activation\", \"LIDBName\", \"LastPorted\", \"DateToExpire\" FROM public.\"VerifiedPhoneNumbers\" " +
                "WHERE \"OrderId\" = @orderId", new { orderId })
                .ConfigureAwait(false);

            return result;
        }

        public async Task<bool> PutAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"VerifiedPhoneNumbers\" SET \"NPA\" = @NPA, \"NXX\" = @NXX, \"XXXX\" = @XXXX, \"IngestedFrom\" = @IngestedFrom, \"DateIngested\" = @DateIngested, \"OrderId\" = @OrderId, \"Wireless\" = @Wireless, \"NumberType\" = @NumberType, \"LocalRoutingNumber\" = @LocalRoutingNumber, \"OperatingCompanyNumber\" = @OperatingCompanyNumber, \"City\" = @City, \"LocalAccessTransportArea\" = @LocalAccessTransportArea, \"RateCenter\" = @RateCenter, \"Province\" = @Province, \"Jurisdiction\" = @Jurisdiction, \"Local\" = @Local, \"LocalExchangeCarrier\" = @LocalExchangeCarrier, \"LocalExchangeCarrierType\" = @LocalExchangeCarrierType, \"ServiceProfileIdentifier\" = @ServiceProfileIdentifier, \"Activation\" = @Activation, \"LIDBName\" = @LIDBName, \"LastPorted\" = @LastPorted, \"DateToExpire\" = @DateToExpire WHERE \"VerifiedPhoneNumberId\" = @VerifiedPhoneNumberId",
                new { NPA, NXX, XXXX, IngestedFrom, DateIngested, OrderId, Wireless, NumberType, LocalRoutingNumber, OperatingCompanyNumber, City, LocalAccessTransportArea, RateCenter, Province, Jurisdiction, Local, LocalExchangeCarrier, LocalExchangeCarrierType, ServiceProfileIdentifier, Activation, LIDBName, LastPorted, DateToExpire, VerifiedPhoneNumberId })
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

        /// <summary>
        /// Added new numbers to the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PostAsync(string connectionString)
        {
            // If anything is null bail out.
            if (NPA < 100 || NXX < 100 || XXXX < 1 || VerifiedDialedNumber == null || City == null || IngestedFrom == null)
            {
                return false;
            }

            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"VerifiedPhoneNumbers\" (\"VerifiedPhoneNumberId\", \"VerifiedDialedNumber\", \"NPA\", \"NXX\", \"XXXX\", \"IngestedFrom\", \"DateIngested\", \"OrderId\", \"Wireless\", \"NumberType\", \"LocalRoutingNumber\", \"OperatingCompanyNumber\", \"City\", \"LocalAccessTransportArea\", \"RateCenter\", \"Province\", \"Jurisdiction\", \"Local\", \"LocalExchangeCarrier\", \"LocalExchangeCarrierType\", \"ServiceProfileIdentifier\", \"Activation\", \"LIDBName\", \"LastPorted\", \"DateToExpire\") " +
                "VALUES (@VerifiedPhoneNumberId, @VerifiedDialedNumber, @NPA, @NXX, @XXXX, @IngestedFrom, @DateIngested, @OrderId, @Wireless, @NumberType, @LocalRoutingNumber, @OperatingCompanyNumber, @City, @LocalAccessTransportArea, @RateCenter, @Province, @Jurisdiction, @Local, @LocalExchangeCarrier, @LocalExchangeCarrierType, @ServiceProfileIdentifier, @Activation, @LIDBName, @LastPorted, @DateToExpire)",
                new { VerifiedPhoneNumberId, VerifiedDialedNumber, NPA, NXX, XXXX, IngestedFrom, DateIngested, OrderId, Wireless, NumberType, LocalRoutingNumber, OperatingCompanyNumber, City, LocalAccessTransportArea, RateCenter, Province, Jurisdiction, Local, LocalExchangeCarrier, LocalExchangeCarrierType, ServiceProfileIdentifier, Activation, LIDBName, LastPorted, DateToExpire })
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

        public async Task<bool> DeleteAsync(string connectionString)
        {
            // Fail fast if we don have the primary key.
            if (VerifiedPhoneNumberId == Guid.Empty)
            {
                return false;
            }

            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"VerifiedPhoneNumbers\" WHERE \"VerifiedPhoneNumberId\" = @VerifiedPhoneNumberId",
                new { VerifiedPhoneNumberId })
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