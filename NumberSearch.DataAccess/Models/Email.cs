﻿using Dapper;

using MailKit.Security;

using MimeKit;

using Npgsql;

using Serilog;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess.Models
{
    public class Email
    {
        public Guid EmailId { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public string PrimaryEmailAddress { get; set; } = string.Empty;
        public string SalesEmailAddress { get; set; } = string.Empty;
        public string CarbonCopy { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string MessageBody { get; set; } = string.Empty;
        public DateTime DateSent { get; set; }
        public bool Completed { get; set; }
        public string CalendarInvite { get; set; } = string.Empty;
        public bool DoNotSend { get; set; }

        /// <summary>
        /// Get all emails.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<Email>> GetAllAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Email>("SELECT \"EmailId\", \"OrderId\", \"PrimaryEmailAddress\", \"CarbonCopy\", \"Subject\", \"MessageBody\", \"DateSent\", \"Completed\", \"SalesEmailAddress\", \"CalendarInvite\", \"DoNotSend\" " +
                "FROM public.\"SentEmails\" ORDER BY \"DateSent\" DESC")
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Get all emails related to a specific order.
        /// </summary>
        /// <param name="OrderId"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<Email>> GetByOrderAsync(Guid OrderId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Email>("SELECT \"EmailId\", \"OrderId\", \"PrimaryEmailAddress\", \"CarbonCopy\", \"Subject\", \"MessageBody\", \"DateSent\", \"Completed\", \"SalesEmailAddress\", \"CalendarInvite\", \"DoNotSend\" " +
                "FROM public.\"SentEmails\" " +
                "WHERE \"OrderId\" = @OrderId",
                new { OrderId })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Get an email by its primary key.
        /// </summary>
        /// <param name="EmailId"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<Email?> GetAsync(Guid EmailId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<Email>("SELECT \"EmailId\", \"OrderId\", \"PrimaryEmailAddress\", \"CarbonCopy\", \"Subject\", \"MessageBody\", \"DateSent\", \"Completed\", \"SalesEmailAddress\", \"CalendarInvite\", \"DoNotSend\" " +
                "FROM public.\"SentEmails\" " +
                "WHERE \"EmailId\" = @EmailId",
                new { EmailId })
                .ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Submit an email to the mail server to be send out.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<bool> SendEmailAsync(string username, string password)
        {
            // If any of the parameters are bad fail fast.
            if (string.IsNullOrWhiteSpace(PrimaryEmailAddress)
                || string.IsNullOrWhiteSpace(Subject)
                || string.IsNullOrWhiteSpace(MessageBody))
            {
                return false;
            }

            DateSent = DateTime.Now;

            // We don't want this to throw exceptions because they are expensive.
            try
            {
                var outboundMessage = new MimeMessage
                {
                    Sender = new MailboxAddress("Number Search", username),
                    Subject = Subject
                };

                var builder = new BodyBuilder
                {
                    HtmlBody = @$"<!DOCTYPE html><html><head><title></title></head><body>{MessageBody}</body></html>"
                };

                var ordersInbox = MailboxAddress.Parse(username);
                var recipient = MailboxAddress.Parse(PrimaryEmailAddress);

                outboundMessage.From.Add(ordersInbox);
                outboundMessage.To.Add(recipient);

                if (!string.IsNullOrWhiteSpace(SalesEmailAddress) && SalesEmailAddress.Contains('@'))
                {
                    var sales = MailboxAddress.Parse(SalesEmailAddress);
                    outboundMessage.Cc.Add(sales);
                }

                if (!string.IsNullOrWhiteSpace(CarbonCopy) && CarbonCopy.Contains('@'))
                {
                    var cc = MailboxAddress.Parse(CarbonCopy);
                    outboundMessage.Cc.Add(cc);
                }

                // If there's an attachment send it, if not just send the body.
                //if (Multipart != null && Multipart.Count > 0)
                //{
                //    builder.Attachments.Add(Multipart);
                //}

                if (!string.IsNullOrWhiteSpace(CalendarInvite))
                {
                    var icsFile = Path.Combine(AppContext.BaseDirectory, "acceleratenetworks.ics");

                    File.WriteAllText(icsFile, CalendarInvite);

                    builder.Attachments.Add(icsFile);
                }

                outboundMessage.Body = builder.ToMessageBody();

                using var smtp = new MailKit.Net.Smtp.SmtpClient();
                smtp.MessageSent += (sender, args) => { };
                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

                await smtp.ConnectAsync("witcher.mxrouting.net", 587, SecureSocketOptions.StartTls).ConfigureAwait(false);
                await smtp.AuthenticateAsync(username, password).ConfigureAwait(false);
                await smtp.SendAsync(outboundMessage).ConfigureAwait(false);
                await smtp.DisconnectAsync(true).ConfigureAwait(false);

                Completed = true;
                return true;
            }
            catch (Exception ex)
            {
                Log.Fatal("[Email] Failed to send email {EmailId}.", EmailId);
                Log.Fatal(ex.Message);
                Log.Fatal(ex.StackTrace ?? "StackTrace was null.");
                Completed = false;
                return false;
            }
        }

        /// <summary>
        /// Save a new email to the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"SentEmails\" (\"OrderId\", \"PrimaryEmailAddress\", \"CarbonCopy\", \"Subject\", \"MessageBody\", \"DateSent\", \"Completed\", \"SalesEmailAddress\", \"CalendarInvite\", \"DoNotSend\" ) " +
                "VALUES ( @OrderId, @PrimaryEmailAddress, @CarbonCopy, @Subject, @MessageBody, @DateSent, @Completed, @SalesEmailAddress, @CalendarInvite, @DoNotSend )",
                new { OrderId, PrimaryEmailAddress, CarbonCopy, Subject, MessageBody, DateSent, Completed, SalesEmailAddress, CalendarInvite, DoNotSend })
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
        /// Update an existing email in the database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> PutAsync(string connectionString)
        {
            // Fail fast if there's no primary key.
            if (EmailId == Guid.Empty)
            {
                return false;
            }

            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"SentEmails\" SET \"OrderId\"= @OrderId, \"PrimaryEmailAddress\"= @PrimaryEmailAddress, \"CarbonCopy\"= @CarbonCopy, \"Subject\"= @Subject, \"MessageBody\"= @MessageBody, \"DateSent\"= @DateSent, \"Completed\" = @Completed, \"SalesEmailAddress\" = @SalesEmailAddress, \"CalendarInvite\" = @CalendarInvite, \"DoNotSend\" = @DoNotSend " +
                "WHERE \"EmailId\" = @EmailId",
                new { OrderId, PrimaryEmailAddress, CarbonCopy, Subject, MessageBody, DateSent, Completed, EmailId, SalesEmailAddress, CalendarInvite, DoNotSend })
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
        /// Delete a specific sent email entry.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(string connectionString)
        {
            // Fail fast if there's no primary key.
            if (EmailId == Guid.Empty)
            {
                return false;
            }

            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"SentEmails\" WHERE \"EmailId\" = @EmailId", new { EmailId })
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