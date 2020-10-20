using Dapper;

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
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public DateTime DateSubmitted { get; set; }
        public string BusinessName { get; set; }
        public string CustomerNotes { get; set; }
        public string BillingClientId { get; set; }
        public string BillingInvoiceId { get; set; }
        public decimal SalesTax { get; set; }
        public bool Quote { get; set; }

        public static async Task<Order> GetByIdAsync(Guid orderId, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<Order>("SELECT \"OrderId\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"DateSubmitted\", \"BusinessName\", \"CustomerNotes\", \"BillingClientId\", \"BillingInvoiceId\", \"Quote\" FROM public.\"Orders\" " +
                "WHERE \"OrderId\" = @orderId ORDER BY \"DateSubmitted\" DESC",
                new { orderId })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<Order>> GetByEmailAsync(string email, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Order>("SELECT \"OrderId\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"DateSubmitted\", \"BusinessName\", \"CustomerNotes\", \"BillingClientId\", \"BillingInvoiceId\", \"Quote\" FROM public.\"Orders\" " +
                "WHERE \"Email\" = @email ORDER BY \"DateSubmitted\" DESC",
                new { email })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<Order>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Order>
                ("SELECT \"OrderId\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"DateSubmitted\", \"BusinessName\", \"CustomerNotes\", \"BillingClientId\", \"BillingInvoiceId\", \"Quote\" FROM public.\"Orders\" ORDER BY \"DateSubmitted\" DESC")
                .ConfigureAwait(false);

            return result;
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            DateSubmitted = DateTime.Now;

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"Orders\"(\"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"DateSubmitted\", \"BusinessName\", \"CustomerNotes\", \"BillingClientId\", \"BillingInvoiceId\", \"Quote\") " +
                "VALUES(@FirstName, @LastName, @Email, @Address, @Address2, @City, @State, @Zip, @DateSubmitted, @BusinessName, @CustomerNotes, @BillingClientId, @BillingInvoiceId, @Quote)",
                new { FirstName, LastName, Email, Address, Address2, City, State, Zip, DateSubmitted, BusinessName, CustomerNotes, BillingClientId, BillingInvoiceId, Quote })
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
            DateSubmitted = DateTime.Now;

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"Orders\" " +
                "SET \"FirstName\" = @FirstName, \"LastName\" = @LastName, \"Email\" = @Email, \"Address\" = @Address, \"Address2\" = @Address2, \"City\" = @City, \"State\" = @State, \"Zip\" = @Zip, \"DateSubmitted\" = @DateSubmitted, \"BusinessName\" = @BusinessName, \"CustomerNotes\" = @CustomerNotes, \"BillingClientId\" = @BillingClientId, \"BillingInvoiceId\" = @BillingInvoiceId, \"Quote\" = @Quote " +
                "WHERE \"OrderId\" = @OrderId",
                new { FirstName, LastName, Email, Address, Address2, City, State, Zip, DateSubmitted, BusinessName, CustomerNotes, BillingClientId, BillingInvoiceId, Quote, OrderId })
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
            if (OrderId == Guid.Empty)
            {
                return false;
            }

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"Orders\" WHERE \"OrderId\" = @OrderId",
                new { OrderId })
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
