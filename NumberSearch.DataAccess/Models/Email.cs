using Dapper;

using MailKit.Security;

using MimeKit;
using MimeKit.Text;

using Npgsql;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class Email
    {
        public Guid EmailId { get; set; }
        public Guid OrderId { get; set; }
        public string PrimaryEmailAddress { get; set; }
        public string CarbonCopy { get; set; }
        public string Subject { get; set; }
        public string MessageBody { get; set; }
        public DateTime DateSent { get; set; }
        public bool Completed { get; set; }

        /// <summary>
        /// Get all emails.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<Email>> GetAllAsync(string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Email>("SELECT \"EmailId\", \"OrderId\", \"PrimaryEmailAddress\", \"CarbonCopy\", \"Subject\", \"MessageBody\", \"DateSent\", \"Completed\" " +
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
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<Email>("SELECT \"EmailId\", \"OrderId\", \"PrimaryEmailAddress\", \"CarbonCopy\", \"Subject\", \"MessageBody\", \"DateSent\", \"Completed\" " +
                "FROM public.\"SentEmails\" " +
                "WHERE \"OrderId\" = @Orderid",
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
        public static async Task<Email> GetAsync(Guid EmailId, string connectionString)
        {
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryFirstOrDefaultAsync<Email>("SELECT \"EmailId\", \"OrderId\", \"PrimaryEmailAddress\", \"CarbonCopy\", \"Subject\", \"MessageBody\", \"DateSent\", \"Completed\" " +
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
                || string.IsNullOrWhiteSpace(CarbonCopy)
                || string.IsNullOrWhiteSpace(Subject)
                || string.IsNullOrWhiteSpace(MessageBody))
            {
                return false;
            }

            DateSent = DateTime.Now;

            // We don't want this to throw execeptions because they are expensive.
            try
            {
                var outboundMessage = new MimeMessage
                {
                    Sender = new MailboxAddress("Number Search", username),
                    Subject = Subject
                };

                outboundMessage.Body = new TextPart(TextFormat.Plain)
                {
                    Text = MessageBody
                };

                var ordersInbox = MailboxAddress.Parse(username);
                var recipient = MailboxAddress.Parse(PrimaryEmailAddress);

                outboundMessage.Cc.Add(ordersInbox);
                outboundMessage.To.Add(recipient);

                using var smtp = new MailKit.Net.Smtp.SmtpClient();
                smtp.MessageSent += (sender, args) => { };
                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

                await smtp.ConnectAsync("mail.seattlemesh.net", 587, SecureSocketOptions.StartTls).ConfigureAwait(false);
                await smtp.AuthenticateAsync(username, password).ConfigureAwait(false);
                await smtp.SendAsync(outboundMessage).ConfigureAwait(false);
                await smtp.DisconnectAsync(true).ConfigureAwait(false);

                Completed = true;
                return true;
            }
            catch
            {
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
            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"SentEmails\" (\"OrderId\", \"PrimaryEmailAddress\", \"CarbonCopy\", \"Subject\", \"MessageBody\", \"DateSent\", \"Completed\") " +
                "VALUES ( @OrderId, @PrimaryEmailAddress, @CarbonCopy, @Subject, @MessageBody, @DateSent, @Completed )", new { OrderId, PrimaryEmailAddress, CarbonCopy, Subject, MessageBody, DateSent, Completed })
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

            using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"SentEmails\" SET \"DateSent\" = @DateSent, \"Completed\" = @Completed WHERE \"EmailId\" = @EmailId", new { DateSent, Completed, EmailId })
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

            using var connection = new NpgsqlConnection(connectionString);

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