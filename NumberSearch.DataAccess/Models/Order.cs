﻿using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class Order
    {
        public Guid OrderId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public DateTime DateSubmitted { get; set; }

        public static async Task<IEnumerable<Order>> GetAsync(Guid orderId, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Order>("SELECT \"OrderId\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"Country\", \"State\", \"Zip\", \"DateSubmitted\" FROM public.\"Orders\" " +
                "WHERE \"OrderId\" = @orderId",
                new { orderId })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<Order>> GetAsync(string email, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Order>("SELECT \"OrderId\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"Country\", \"State\", \"Zip\", \"DateSubmitted\" FROM public.\"Orders\" " +
                "WHERE \"Email\" = @email ORDER BY \"DateSubmitted\" DESC",
                new { email })
                .ConfigureAwait(false);

            return result;
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            DateSubmitted = DateTime.Now;

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"Orders\"(\"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"Country\", \"State\", \"Zip\", \"DateSubmitted\") " +
                "VALUES(@FirstName, @LastName, @Email, @Address, @Address2, @Country, @State, @Zip, @DateSubmitted)",
                new { FirstName, LastName, Email, Address, Address2, Country, State, Zip, DateSubmitted })
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