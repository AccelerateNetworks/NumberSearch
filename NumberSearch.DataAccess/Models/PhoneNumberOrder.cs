﻿using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class PhoneNumberOrder
    {
        public Guid Id { get; set; }
        public string DialedNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public DateTime DateSubmitted { get; set; }

        public static async Task<IEnumerable<PhoneNumberOrder>> GetAsync(string dialedNumber, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"SELECT \"Id\", \"DialedNumber\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"Country\", \"State\", \"Zip\", \"DateSubmitted\" FROM public.\"Orders\" WHERE \"DialedNumber\" = '{dialedNumber}'";

            var result = await connection.QueryAsync<PhoneNumberOrder>(sql).ConfigureAwait(false);

            return result;
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            DateSubmitted = DateTime.Now;

            using var connection = new NpgsqlConnection(connectionString);

            string sql = $"INSERT INTO public.\"Orders\"(\"DialedNumber\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"Country\", \"State\", \"Zip\", \"DateSubmitted\") VALUES('{DialedNumber}', '{FirstName}', '{LastName}', '{Email}', '{Address}', '{Address2}', '{Country}', '{State}', '{Zip}', '{DateSubmitted}')";

            var result = await connection.ExecuteAsync(sql).ConfigureAwait(false);

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