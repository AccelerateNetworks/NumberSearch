﻿using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class EmergencyInformation
    {
        public Guid EmergencyInformationId { get; set; }
        public string DialedNumber { get; set; }
        public string IngestedFrom { get; set; }
        public DateTime DateIngested { get; set; }
        public string TeliId { get; set; }
        public string FullName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string UnitType { get; set; }
        public string UnitNumber { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string AlertGroup { get; set; }
        public string Note { get; set; }

        public static async Task<IEnumerable<EmergencyInformation>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<EmergencyInformation>("SELECT \"EmergencyInformationId\", \"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"TeliId\", \"FullName\", \"Address\", \"City\", \"State\", \"Zip\", \"UnitType\", \"UnitNumber\", \"CreatedDate\", \"ModifyDate\", \"AlertGroup\", \"Note\" FROM public.\"EmergencyInformation\"")
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<EmergencyInformation>> GetByDialedNumberAsync(string dialedNumber, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<EmergencyInformation>("SELECT \"EmergencyInformationId\", \"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"TeliId\", \"FullName\", \"Address\", \"City\", \"State\", \"Zip\", \"UnitType\", \"UnitNumber\", \"CreatedDate\", \"ModifyDate\", \"AlertGroup\", \"Note\" FROM public.\"EmergencyInformation\" WHERE \"DialedNumber\" = @dialedNumber", new { dialedNumber })
                .ConfigureAwait(false);

            return result;
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"EmergencyInformation\" (\"DialedNumber\", \"IngestedFrom\", \"DateIngested\", \"TeliId\", \"FullName\", \"Address\", \"City\", \"State\", \"Zip\", \"UnitType\", \"UnitNumber\", \"CreatedDate\", \"ModifyDate\", \"AlertGroup\", \"Note\") " +
                "VALUES ( @DialedNumber, @IngestedFrom, @DateIngested, @TeliId, @FullName, @Address, @City, @State, @Zip, @UnitType, @UnitNumber, @CreatedDate, @ModifyDate, @AlertGroup, @Note )",
                new { DialedNumber, IngestedFrom, DateIngested, TeliId, FullName, Address, City, State, Zip, UnitType, UnitNumber, CreatedDate, ModifyDate, AlertGroup, Note })
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

        public async Task<bool> PutAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"EmergencyInformation\" SET \"DialedNumber\" = @DialedNumber, \"IngestedFrom\" = @IngestedFrom, \"DateIngested\" = @DateIngested, \"TeliId\" = @TeliId, \"FullName\" = @FullName, \"Address\" = @Address, \"City\" = @City, \"State\" = @State, \"Zip\" = @Zip, \"UnitType\" = @UnitType, \"UnitNumber\" = @UnitNumber, \"CreatedDate\" = @CreatedDate, \"ModifyDate\" = @ModifyDate, \"AlertGroup\" = @AlertGroup, \"Note\" = @Note WHERE \"EmergencyInformationId\" = @EmergencyInformationId",
                new { DialedNumber, IngestedFrom, DateIngested, TeliId, FullName, Address, City, State, Zip, UnitType, UnitNumber, CreatedDate, ModifyDate, AlertGroup, Note, EmergencyInformationId })
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
            if (EmergencyInformationId == Guid.Empty)
            {
                return false;
            }

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"EmergencyInformation\" WHERE \"EmergencyInformationId\" = @EmergencyInformationId",
                new { EmergencyInformationId })
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