﻿using Dapper;

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
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string BillingPhone { get; set; }
        public string LocationType { get; set; }
        public string BusinessContact { get; set; }
        public string BusinessName { get; set; }
        public string ProviderAccountNumber { get; set; }
        public string ProviderPIN { get; set; }
        public bool PartialPort { get; set; }
        public string PartialPortDescription { get; set; }
        public bool WirelessNumber { get; set; }
        public string CallerId { get; set; }
        // Only used in the form
        public IFormFile BillImage { get; set; }
        public string BillImagePath { get; set; }
        public string BillImageFileType { get; set; }
        public DateTime DateSubmitted { get; set; }
        public string ResidentialFirstName { get; set; }
        public string ResidentialLastName { get; set; }
        public string TeliId { get; set; }
        public string RequestStatus { get; set; }
        public bool Completed { get; set; }
        public DateTime? DateCompleted { get; set; }
        public DateTime? DateUpdated { get; set; }
        public string VendorSubmittedTo { get; set; }

        /// <summary>
        /// Get all of the Port Requests from the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PortRequest>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryAsync<PortRequest>("SELECT \"PortRequestId\", \"OrderId\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"BillingPhone\", \"LocationType\", \"BusinessContact\", \"BusinessName\", \"ProviderAccountNumber\", \"ProviderPIN\", \"PartialPort\", \"PartialPortDescription\", \"WirelessNumber\", \"CallerId\", \"BillImagePath\", \"BillImageFileType\", \"DateSubmitted\", \"ResidentialFirstName\", \"ResidentialLastName\", \"TeliId\", \"RequestStatus\", \"Completed\", \"DateCompleted\", \"DateUpdated\", \"VendorSubmittedTo\" FROM public.\"PortRequests\"")
                .ConfigureAwait(false);
        }


        /// <summary>
        /// Get port request information by the Id of the parent order.
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<PortRequest> GetByOrderIdAsync(Guid orderId, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            return await connection
                .QueryFirstOrDefaultAsync<PortRequest>("SELECT \"PortRequestId\", \"OrderId\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"BillingPhone\", \"LocationType\", \"BusinessContact\", \"BusinessName\", \"ProviderAccountNumber\", \"ProviderPIN\", \"PartialPort\", \"PartialPortDescription\", \"WirelessNumber\", \"CallerId\", \"BillImagePath\", \"BillImageFileType\", \"DateSubmitted\", \"ResidentialFirstName\", \"ResidentialLastName\", \"TeliId\", \"RequestStatus\", \"Completed\", \"DateCompleted\", \"DateUpdated\", \"VendorSubmittedTo\" FROM public.\"PortRequests\" WHERE \"OrderId\" = @orderId",
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

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"PortRequests\"(\"OrderId\", \"Address\", \"Address2\", \"City\", \"State\", \"Zip\", \"BillingPhone\", \"LocationType\", \"BusinessContact\", \"BusinessName\", \"ProviderAccountNumber\", \"ProviderPIN\", \"PartialPort\", \"PartialPortDescription\", \"WirelessNumber\", \"CallerId\", \"BillImagePath\", \"BillImageFileType\", \"DateSubmitted\", \"ResidentialFirstName\", \"ResidentialLastName\", \"TeliId\", \"RequestStatus\", \"Completed\", \"DateCompleted\", \"DateUpdated\", \"VendorSubmittedTo\") " +
                "VALUES( @OrderId, @Address, @Address2, @City, @State, @Zip, @BillingPhone, @LocationType, @BusinessContact, @BusinessName, @ProviderAccountNumber, @ProviderPIN, @PartialPort, @PartialPortDescription, @WirelessNumber, @CallerId, @BillImagePath, @BillImageFileType, @DateSubmitted, @ResidentialFirstName, @ResidentialLastName, @TeliId, @RequestStatus, @Completed, @DateCompleted, @DateUpdated, @VendorSubmittedTo)",
                new { OrderId, Address, Address2, City, State, Zip, BillingPhone, LocationType, BusinessContact, BusinessName, ProviderAccountNumber, ProviderPIN, PartialPort, PartialPortDescription, WirelessNumber, CallerId, BillImagePath, BillImageFileType, DateSubmitted, ResidentialFirstName, ResidentialLastName, TeliId, RequestStatus, Completed, DateCompleted, DateUpdated, VendorSubmittedTo })
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
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"PortRequests\" SET \"OrderId\" = @OrderId, \"Address\" = @Address, \"Address2\" = @Address2, \"City\" = @City, \"State\" = @State, \"Zip\" = @Zip, \"BillingPhone\" = @BillingPhone, \"LocationType\" = @LocationType, \"BusinessContact\" = @BusinessContact, \"BusinessName\" = @BusinessName, \"ProviderAccountNumber\" = @ProviderAccountNumber, \"ProviderPIN\" = @ProviderPIN, \"PartialPort\" = @PartialPort, \"PartialPortDescription\" = @PartialPortDescription, \"WirelessNumber\" = @WirelessNumber, \"CallerId\" = @CallerId, \"BillImagePath\" = @BillImagePath, \"BillImageFileType\" = @BillImageFileType, \"DateSubmitted\" = @DateSubmitted, \"ResidentialFirstName\" = @ResidentialFirstName, \"ResidentialLastName\" = @ResidentialLastName, \"TeliId\" = @TeliId, \"RequestStatus\" = @RequestStatus, \"Completed\" = @Completed, \"DateCompleted\" = @DateCompleted, \"DateUpdated\" = @DateUpdated, \"VendorSubmittedTo\" = @VendorSubmittedTo WHERE \"PortRequestId\" = @PortRequestId",
                new { OrderId, Address, Address2, City, State, Zip, BillingPhone, LocationType, BusinessContact, BusinessName, ProviderAccountNumber, ProviderPIN, PartialPort, PartialPortDescription, WirelessNumber, CallerId, BillImagePath, BillImageFileType, DateSubmitted, PortRequestId, ResidentialFirstName, ResidentialLastName, TeliId, RequestStatus, Completed, DateCompleted, DateUpdated, VendorSubmittedTo })
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
    }
}