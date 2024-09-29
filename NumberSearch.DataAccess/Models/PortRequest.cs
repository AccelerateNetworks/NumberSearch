using Dapper;

using Microsoft.AspNetCore.Http;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class PortRequest
    {
        public Guid PortRequestId { get; set; }
        public Guid OrderId { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Address2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string BillingPhone { get; set; } = string.Empty;
        public string LocationType { get; set; } = string.Empty;
        public string BusinessContact { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string ProviderAccountNumber { get; set; } = string.Empty;
        public string ProviderPIN { get; set; } = string.Empty;
        public bool PartialPort { get; set; }
        public string PartialPortDescription { get; set; } = string.Empty;
        public bool WirelessNumber { get; set; }
        public string CallerId { get; set; } = string.Empty;
        // Only used in the form
        public IFormFile? BillImage { get; set; }
        public string BillImagePath { get; set; } = string.Empty;
        public string BillImageFileType { get; set; } = string.Empty;
        public DateTime DateSubmitted { get; set; }
        public string ResidentialFirstName { get; set; } = string.Empty;
        public string ResidentialLastName { get; set; } = string.Empty;
        public string TeliId { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
        public bool Completed { get; set; }
        public DateTime? DateCompleted { get; set; }
        public DateTime? DateUpdated { get; set; }
        public string VendorSubmittedTo { get; set; } = string.Empty;
        public string BulkVSId { get; set; } = string.Empty;

        /// <summary>
        /// Get all of the Port Requests from the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PortRequest>> GetAllAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryAsync<PortRequest>("SELECT \"PortRequestId\", \"OrderId\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"BillingPhone\", \"LocationType\", \"BusinessContact\", \"BusinessName\", \"ProviderAccountNumber\", \"ProviderPIN\", \"PartialPort\", \"PartialPortDescription\", \"WirelessNumber\", \"CallerId\", \"BillImagePath\", \"BillImageFileType\", \"DateSubmitted\", \"ResidentialFirstName\", \"ResidentialLastName\", \"TeliId\", \"RequestStatus\", \"Completed\", \"DateCompleted\", \"DateUpdated\", \"VendorSubmittedTo\", \"BulkVSId\" FROM public.\"PortRequests\"")
                .ConfigureAwait(false);
        }


        /// <summary>
        /// Get port request information by the Id of the parent order.
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<PortRequest?> GetByOrderIdAsync(Guid orderId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryFirstOrDefaultAsync<PortRequest>("SELECT \"PortRequestId\", \"OrderId\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"BillingPhone\", \"LocationType\", \"BusinessContact\", \"BusinessName\", \"ProviderAccountNumber\", \"ProviderPIN\", \"PartialPort\", \"PartialPortDescription\", \"WirelessNumber\", \"CallerId\", \"BillImagePath\", \"BillImageFileType\", \"DateSubmitted\", \"ResidentialFirstName\", \"ResidentialLastName\", \"TeliId\", \"RequestStatus\", \"Completed\", \"DateCompleted\", \"DateUpdated\", \"VendorSubmittedTo\", \"BulkVSId\" FROM public.\"PortRequests\" WHERE \"OrderId\" = @orderId",
                new { orderId })
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Add a Port Request to the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PostAsync(string connectionString)
        {
            DateSubmitted = DateTime.Now;

            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"PortRequests\"(\"PortRequestId\", \"OrderId\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"BillingPhone\", \"LocationType\", \"BusinessContact\", \"BusinessName\", \"ProviderAccountNumber\", \"ProviderPIN\", \"PartialPort\", \"PartialPortDescription\", \"WirelessNumber\", \"CallerId\", \"BillImagePath\", \"BillImageFileType\", \"DateSubmitted\", \"ResidentialFirstName\", \"ResidentialLastName\", \"TeliId\", \"RequestStatus\", \"Completed\", \"DateCompleted\", \"DateUpdated\", \"VendorSubmittedTo\", \"BulkVSId\") " +
                "VALUES(@PortRequestId, @OrderId, @Address, @Address2, @City, @State, @Zip, @BillingPhone, @LocationType, @BusinessContact, @BusinessName, @ProviderAccountNumber, @ProviderPIN, @PartialPort, @PartialPortDescription, @WirelessNumber, @CallerId, @BillImagePath, @BillImageFileType, @DateSubmitted, @ResidentialFirstName, @ResidentialLastName, @TeliId, @RequestStatus, @Completed, @DateCompleted, @DateUpdated, @VendorSubmittedTo, @BulkVSId)",
                new { PortRequestId, OrderId, Address, Address2, City, State, Zip, BillingPhone, LocationType, BusinessContact, BusinessName, ProviderAccountNumber, ProviderPIN, PartialPort, PartialPortDescription, WirelessNumber, CallerId, BillImagePath, BillImageFileType, DateSubmitted, ResidentialFirstName, ResidentialLastName, TeliId, RequestStatus, Completed, DateCompleted, DateUpdated, VendorSubmittedTo, BulkVSId })
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
        /// Update an existing port request.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PutAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"PortRequests\" SET \"OrderId\" = @OrderId, \"Address\" = @Address, \"Address2\" = @Address2, \"City\" = @City, \"State\" = @State, \"Zip\" = @Zip, " +
                "\"BillingPhone\" = @BillingPhone, \"LocationType\" = @LocationType, \"BusinessContact\" = @BusinessContact, \"BusinessName\" = @BusinessName, " +
                "\"ProviderAccountNumber\" = @ProviderAccountNumber, \"ProviderPIN\" = @ProviderPIN, \"PartialPort\" = @PartialPort, \"PartialPortDescription\" = @PartialPortDescription, " +
                "\"WirelessNumber\" = @WirelessNumber, \"CallerId\" = @CallerId, \"BillImagePath\" = @BillImagePath, \"BillImageFileType\" = @BillImageFileType, \"DateSubmitted\" = @DateSubmitted, " +
                "\"ResidentialFirstName\" = @ResidentialFirstName, \"ResidentialLastName\" = @ResidentialLastName, \"TeliId\" = @TeliId, \"RequestStatus\" = @RequestStatus, \"Completed\" = @Completed, " +
                "\"DateCompleted\" = @DateCompleted, \"DateUpdated\" = @DateUpdated, \"VendorSubmittedTo\" = @VendorSubmittedTo, \"BulkVSId\" = @BulkVSId WHERE \"PortRequestId\" = @PortRequestId",
                new
                {
                    OrderId,
                    Address,
                    Address2,
                    City,
                    State,
                    Zip,
                    BillingPhone,
                    LocationType,
                    BusinessContact,
                    BusinessName,
                    ProviderAccountNumber,
                    ProviderPIN,
                    PartialPort,
                    PartialPortDescription,
                    WirelessNumber,
                    CallerId,
                    BillImagePath,
                    BillImageFileType,
                    DateSubmitted,
                    ResidentialFirstName,
                    ResidentialLastName,
                    TeliId,
                    RequestStatus,
                    Completed,
                    DateCompleted,
                    DateUpdated,
                    VendorSubmittedTo,
                    BulkVSId,
                    PortRequestId
                })
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
                .ExecuteAsync("DELETE FROM public.\"PortRequests\" WHERE \"OrderId\" = @OrderId",
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

        public async Task<bool> DeleteByIdAsync(string connectionString)
        {
            // Fail fast if we don have the primary key.
            if (OrderId == Guid.Empty)
            {
                return false;
            }

            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"PortRequests\" WHERE \"PortRequestId\" = @PortRequestId",
                new { PortRequestId })
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