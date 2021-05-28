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
        public string BillingInvoiceReoccuringId { get; set; }
        public string SalesEmail { get; set; }
        public bool BackgroundWorkCompleted { get; set; }
        public bool Completed { get; set; }
        public DateTime? InstallDate { get; set; }
        public string UpfrontInvoiceLink { get; set; }
        public string ReoccuringInvoiceLink { get; set; }
        public bool OnsiteInstallation { get; set; }
        public string AddressUnitType { get; set; }
        public string AddressUnitNumber { get; set; }

        public static async Task<Order> GetByIdAsync(Guid orderId, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<Order>("SELECT \"OrderId\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"DateSubmitted\", \"BusinessName\", \"CustomerNotes\", \"BillingClientId\", \"BillingInvoiceId\", \"Quote\", \"BillingInvoiceReoccuringId\", \"SalesEmail\", \"BackgroundWorkCompleted\", \"Completed\", \"InstallDate\", \"UpfrontInvoiceLink\", \"ReoccuringInvoiceLink\", \"OnsiteInstallation\", \"AddressUnitType\", \"AddressUnitNumber\" FROM public.\"Orders\" " +
                "WHERE \"OrderId\" = @orderId " +
                "ORDER BY \"DateSubmitted\" DESC",
                new { orderId })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<Order>> GetByEmailAsync(string email, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Order>("SELECT \"OrderId\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"DateSubmitted\", \"BusinessName\", \"CustomerNotes\", \"BillingClientId\", \"BillingInvoiceId\", \"Quote\", \"BillingInvoiceReoccuringId\", \"SalesEmail\", \"BackgroundWorkCompleted\", \"Completed\", \"InstallDate\", \"UpfrontInvoiceLink\", \"ReoccuringInvoiceLink\", \"OnsiteInstallation\", \"AddressUnitType\", \"AddressUnitNumber\" FROM public.\"Orders\" " +
                "WHERE \"Email\" = @email " +
                "ORDER BY \"DateSubmitted\" DESC",
                new { email })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<Order>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Order>
                ("SELECT \"OrderId\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"DateSubmitted\", \"BusinessName\", \"CustomerNotes\", \"BillingClientId\", \"BillingInvoiceId\", \"Quote\", \"BillingInvoiceReoccuringId\", \"SalesEmail\", \"BackgroundWorkCompleted\", \"Completed\", \"InstallDate\", \"UpfrontInvoiceLink\", \"ReoccuringInvoiceLink\", \"OnsiteInstallation\", \"AddressUnitType\", \"AddressUnitNumber\" " +
                "FROM public.\"Orders\" " +
                "ORDER BY \"DateSubmitted\" DESC")
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<Order>> GetByBackGroundworkNotCompletedAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Order>
                ("SELECT \"OrderId\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"DateSubmitted\", \"BusinessName\", \"CustomerNotes\", \"BillingClientId\", \"BillingInvoiceId\", \"Quote\", \"BillingInvoiceReoccuringId\", \"SalesEmail\", \"BackgroundWorkCompleted\", \"Completed\", \"InstallDate\", \"UpfrontInvoiceLink\", \"ReoccuringInvoiceLink\", \"OnsiteInstallation\", \"AddressUnitType\", \"AddressUnitNumber\" " +
                "FROM public.\"Orders\" " +
                "WHERE \"BackgroundWorkCompleted\" = false " +
                "ORDER BY \"DateSubmitted\" DESC")
                .ConfigureAwait(false);

            return result;
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"Orders\"(\"OrderId\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"DateSubmitted\", \"BusinessName\", \"CustomerNotes\", \"BillingClientId\", \"BillingInvoiceId\", \"Quote\", \"BillingInvoiceReoccuringId\", \"SalesEmail\", \"BackgroundWorkCompleted\", \"Completed\", \"InstallDate\", \"UpfrontInvoiceLink\", \"ReoccuringInvoiceLink\", \"OnsiteInstallation\", \"AddressUnitType\", \"AddressUnitNumber\") " +
                "VALUES(@OrderID, @FirstName, @LastName, @Email, @Address, @Address2, @City, @State, @Zip, @DateSubmitted, @BusinessName, @CustomerNotes, @BillingClientId, @BillingInvoiceId, @Quote, @BillingInvoiceReoccuringId, @SalesEmail, @BackgroundWorkCompleted, @Completed, @InstallDate, @UpfrontInvoiceLink, @ReoccuringInvoiceLink, @OnsiteInstallation, @AddressUnitType, @AddressUnitNumber)",
                new { OrderId, FirstName, LastName, Email, Address, Address2, City, State, Zip, DateSubmitted, BusinessName, CustomerNotes, BillingClientId, BillingInvoiceId, Quote, BillingInvoiceReoccuringId, SalesEmail, BackgroundWorkCompleted, Completed, InstallDate, UpfrontInvoiceLink, ReoccuringInvoiceLink, OnsiteInstallation, AddressUnitType, AddressUnitNumber })
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
                .ExecuteAsync("UPDATE public.\"Orders\" " +
                "SET \"FirstName\" = @FirstName, \"LastName\" = @LastName, \"Email\" = @Email, \"Address\" = @Address, \"Address2\" = @Address2, \"City\" = @City, \"State\" = @State, \"Zip\" = @Zip, \"DateSubmitted\" = @DateSubmitted, \"BusinessName\" = @BusinessName, \"CustomerNotes\" = @CustomerNotes, \"BillingClientId\" = @BillingClientId, \"BillingInvoiceId\" = @BillingInvoiceId, \"Quote\" = @Quote, \"BillingInvoiceReoccuringId\" = @BillingInvoiceReoccuringId, \"SalesEmail\" = @SalesEmail, \"BackgroundWorkCompleted\" = @BackgroundWorkCompleted, \"Completed\" = @Completed, \"InstallDate\" = @InstallDate, \"UpfrontInvoiceLink\" = @UpfrontInvoiceLink, \"ReoccuringInvoiceLink\" = @ReoccuringInvoiceLink, \"OnsiteInstallation\" = @OnsiteInstallation, \"AddressUnitType\" = @AddressUnitType, \"AddressUnitNumber\" = @AddressUnitNumber " +
                "WHERE \"OrderId\" = @OrderId",
                new { FirstName, LastName, Email, Address, Address2, City, State, Zip, DateSubmitted, BusinessName, CustomerNotes, BillingClientId, BillingInvoiceId, Quote, BillingInvoiceReoccuringId, SalesEmail, BackgroundWorkCompleted, OrderId, Completed, InstallDate, UpfrontInvoiceLink, ReoccuringInvoiceLink, OnsiteInstallation, @AddressUnitType, @AddressUnitNumber })
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