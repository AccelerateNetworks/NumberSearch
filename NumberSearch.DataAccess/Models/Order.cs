using Dapper;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class Order
    {
        public Guid OrderId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactPhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Address2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public DateTime DateSubmitted { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string CustomerNotes { get; set; } = string.Empty;
        public string BillingClientId { get; set; } = string.Empty;
        public string BillingInvoiceId { get; set; } = string.Empty;
        public decimal SalesTax { get; set; }
        public bool Quote { get; set; }
        public string BillingInvoiceReoccuringId { get; set; } = string.Empty;
        public string SalesEmail { get; set; } = string.Empty;
        public bool BackgroundWorkCompleted { get; set; }
        public bool Completed { get; set; }
        public DateTime? InstallDate { get; set; }
        public string UpfrontInvoiceLink { get; set; } = string.Empty;
        public string ReoccuringInvoiceLink { get; set; } = string.Empty;
        public bool OnsiteInstallation { get; set; }
        public string AddressUnitType { get; set; } = string.Empty;
        public string AddressUnitNumber { get; set; } = string.Empty;
        public string UnparsedAddress { get; set; } = string.Empty;
        public Guid? MergedOrderId { get; set; }
        public string E911ServiceNumber { get; set; } = string.Empty;
        public DateTime? DateConvertedFromQuote { get; set; } = null;
        public DateTime? DateCompleted { get; set; } = null;

        public static async Task<Order> GetByIdAsync(Guid orderId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<Order>("SELECT \"OrderId\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"DateSubmitted\", \"BusinessName\", \"CustomerNotes\", \"BillingClientId\", \"BillingInvoiceId\", \"Quote\", \"BillingInvoiceReoccuringId\", \"SalesEmail\", \"BackgroundWorkCompleted\", \"Completed\", \"InstallDate\", \"UpfrontInvoiceLink\", \"ReoccuringInvoiceLink\", \"OnsiteInstallation\", \"AddressUnitType\", \"AddressUnitNumber\", \"UnparsedAddress\", \"MergedOrderId\", \"E911ServiceNumber\", \"DateConvertedFromQuote\", \"DateCompleted\", \"ContactPhoneNumber\" FROM public.\"Orders\" " +
                "WHERE \"OrderId\" = @orderId " +
                "ORDER BY \"DateSubmitted\" DESC",
                new { orderId })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<Order>> GetByEmailAsync(string email, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Order>("SELECT \"OrderId\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"DateSubmitted\", \"BusinessName\", \"CustomerNotes\", \"BillingClientId\", \"BillingInvoiceId\", \"Quote\", \"BillingInvoiceReoccuringId\", \"SalesEmail\", \"BackgroundWorkCompleted\", \"Completed\", \"InstallDate\", \"UpfrontInvoiceLink\", \"ReoccuringInvoiceLink\", \"OnsiteInstallation\", \"AddressUnitType\", \"AddressUnitNumber\", \"UnparsedAddress\", \"MergedOrderId\", \"E911ServiceNumber\", \"DateConvertedFromQuote\", \"DateCompleted\", \"ContactPhoneNumber\" FROM public.\"Orders\" " +
                "WHERE \"Email\" = @email " +
                "ORDER BY \"DateSubmitted\" DESC",
                new { email })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<Order>> GetAllAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Order>
                ("SELECT \"OrderId\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"DateSubmitted\", \"BusinessName\", \"CustomerNotes\", \"BillingClientId\", \"BillingInvoiceId\", \"Quote\", \"BillingInvoiceReoccuringId\", \"SalesEmail\", \"BackgroundWorkCompleted\", \"Completed\", \"InstallDate\", \"UpfrontInvoiceLink\", \"ReoccuringInvoiceLink\", \"OnsiteInstallation\", \"AddressUnitType\", \"AddressUnitNumber\", \"UnparsedAddress\", \"MergedOrderId\", \"E911ServiceNumber\", \"DateConvertedFromQuote\", \"DateCompleted\", \"ContactPhoneNumber\" " +
                "FROM public.\"Orders\" " +
                "ORDER BY \"DateSubmitted\" DESC")
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<IEnumerable<Order>> GetByBackGroundworkNotCompletedAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Order>
                ("SELECT \"OrderId\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"DateSubmitted\", \"BusinessName\", \"CustomerNotes\", \"BillingClientId\", \"BillingInvoiceId\", \"Quote\", \"BillingInvoiceReoccuringId\", \"SalesEmail\", \"BackgroundWorkCompleted\", \"Completed\", \"InstallDate\", \"UpfrontInvoiceLink\", \"ReoccuringInvoiceLink\", \"OnsiteInstallation\", \"AddressUnitType\", \"AddressUnitNumber\", \"UnparsedAddress\", \"MergedOrderId\", \"E911ServiceNumber\", \"DateConvertedFromQuote\", \"DateCompleted\", \"ContactPhoneNumber\" " +
                "FROM public.\"Orders\" " +
                "WHERE \"BackgroundWorkCompleted\" = false " +
                "ORDER BY \"DateSubmitted\" DESC")
                .ConfigureAwait(false);

            return result;
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"Orders\"(\"OrderId\", \"FirstName\", \"LastName\", \"Email\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"DateSubmitted\", \"BusinessName\", \"CustomerNotes\", \"BillingClientId\", \"BillingInvoiceId\", \"Quote\", \"BillingInvoiceReoccuringId\", \"SalesEmail\", \"BackgroundWorkCompleted\", \"Completed\", \"InstallDate\", \"UpfrontInvoiceLink\", \"ReoccuringInvoiceLink\", \"OnsiteInstallation\", \"AddressUnitType\", \"AddressUnitNumber\", \"UnparsedAddress\", \"MergedOrderId\", \"E911ServiceNumber\", \"DateConvertedFromQuote\", \"DateCompleted\", \"ContactPhoneNumber\" ) " +
                "VALUES(@OrderId, @FirstName, @LastName, @Email, @Address, @Address2, @City, @State, @Zip, @DateSubmitted, @BusinessName, @CustomerNotes, @BillingClientId, @BillingInvoiceId, @Quote, @BillingInvoiceReoccuringId, @SalesEmail, @BackgroundWorkCompleted, @Completed, @InstallDate, @UpfrontInvoiceLink, @ReoccuringInvoiceLink, @OnsiteInstallation, @AddressUnitType, @AddressUnitNumber, @UnparsedAddress, @MergedOrderId, @E911ServiceNumber, @DateConvertedFromQuote, @DateCompleted, @ContactPhoneNumber )",
                new { OrderId, FirstName, LastName, Email, Address, Address2, City, State, Zip, DateSubmitted, BusinessName, CustomerNotes, BillingClientId, BillingInvoiceId, Quote, BillingInvoiceReoccuringId, SalesEmail, BackgroundWorkCompleted, Completed, InstallDate, UpfrontInvoiceLink, ReoccuringInvoiceLink, OnsiteInstallation, AddressUnitType, AddressUnitNumber, UnparsedAddress, MergedOrderId, E911ServiceNumber, DateConvertedFromQuote, DateCompleted, ContactPhoneNumber })
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
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"Orders\" " +
                "SET \"FirstName\" = @FirstName, \"LastName\" = @LastName, \"Email\" = @Email, \"Address\" = @Address, \"Address2\" = @Address2, \"City\" = @City, \"State\" = @State, \"Zip\" = @Zip, \"DateSubmitted\" = @DateSubmitted, \"BusinessName\" = @BusinessName, \"CustomerNotes\" = @CustomerNotes, \"BillingClientId\" = @BillingClientId, \"BillingInvoiceId\" = @BillingInvoiceId, \"Quote\" = @Quote, \"BillingInvoiceReoccuringId\" = @BillingInvoiceReoccuringId, \"SalesEmail\" = @SalesEmail, \"BackgroundWorkCompleted\" = @BackgroundWorkCompleted, \"Completed\" = @Completed, \"InstallDate\" = @InstallDate, \"UpfrontInvoiceLink\" = @UpfrontInvoiceLink, \"ReoccuringInvoiceLink\" = @ReoccuringInvoiceLink, \"OnsiteInstallation\" = @OnsiteInstallation, \"AddressUnitType\" = @AddressUnitType, \"AddressUnitNumber\" = @AddressUnitNumber, \"UnparsedAddress\" = @UnparsedAddress, \"MergedOrderId\" = @MergedOrderId, \"E911ServiceNumber\" = @E911ServiceNumber, \"DateConvertedFromQuote\" = @DateConvertedFromQuote, \"DateCompleted\" = @DateCompleted, \"ContactPhoneNumber\" = @ContactPhoneNumber " +
                "WHERE \"OrderId\" = @OrderId",
                new { FirstName, LastName, Email, Address, Address2, City, State, Zip, DateSubmitted, BusinessName, CustomerNotes, BillingClientId, BillingInvoiceId, Quote, BillingInvoiceReoccuringId, SalesEmail, BackgroundWorkCompleted, Completed, InstallDate, UpfrontInvoiceLink, ReoccuringInvoiceLink, OnsiteInstallation, AddressUnitType, AddressUnitNumber, UnparsedAddress, MergedOrderId, E911ServiceNumber, DateConvertedFromQuote, DateCompleted, ContactPhoneNumber, OrderId })
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

            await using var connection = new NpgsqlConnection(connectionString);

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

        public static string GetStatus(Order order, IEnumerable<ProductOrder> productOrders, PortRequest portRequest)
        {
            // The order is completed, we're good.
            if (order?.Completed is true)
            {
                return "🎉 Done, Good Job";
            }
            // The order is not completed, and is stale.
            else if (order?.Completed is not true && order?.DateSubmitted < DateTime.Now.AddDays(-14))
            {
                return "⭕ Contact the Customer, the order is Stale";
            }
            else if (order?.Completed is not true)
            {
                if (order?.Quote is not null && order.Quote)
                {
                    return $"⭕ Pending Quote Approval";
                }
                else if (portRequest is null && productOrders.Where(x => x.PortedPhoneNumberId.HasValue is true).Any() && order is not null)
                {
                    return $"⭕ Get the Porting information from the Customer";
                }
                else if (order is not null && portRequest is not null)
                {
                    if (portRequest?.Completed is true)
                    {
                        if (order.OnsiteInstallation is true && order?.OrderId is not null)
                        {
                            return $"⭕ Install the customer's hardware onsite {order?.InstallDate.GetValueOrDefault().ToShortDateString()}";
                        }
                        else
                        {
                            return $"⭕ Ship the hardware to the customer for self-install";
                        }
                    }
                    else
                    {
                        return "⭕ Port the Customer's Numbers to our Network";
                    }
                }
                else
                {
                    if (order?.OnsiteInstallation is true)
                    {
                        return $"⭕ Install the customer's hardware onsite";
                    }
                    else
                    {
                        return $"⭕ Ship the hardware to the customer for self-install";
                    }
                }
            }
            else
            {
                return "Scooby Doo";
            }
        }
    }
}