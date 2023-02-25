using MailKit.Security;

using MimeKit;

using Serilog;

using System;
using System.IO;
using System.Threading.Tasks;

namespace AccelerateNetworks.Operations.Services;

public class SendEmail
{
    /// <summary>
    /// Submit an email to the mail server to be send out.
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public static async Task<bool> SendEmailAsync(SentEmail email, string username, string password)
    {
        // If any of the parameters are bad fail fast.
        if (string.IsNullOrWhiteSpace(email.PrimaryEmailAddress)
            || string.IsNullOrWhiteSpace(email.CarbonCopy)
            || string.IsNullOrWhiteSpace(email.Subject)
            || string.IsNullOrWhiteSpace(email.MessageBody))
        {
            return false;
        }

        email.DateSent = DateTime.Now;

        // We don't want this to throw execeptions because they are expensive.
        try
        {
            var outboundMessage = new MimeMessage
            {
                Sender = new MailboxAddress("Number Search", username),
                Subject = email.Subject
            };

            var builder = new BodyBuilder
            {
                HtmlBody = @$"<!DOCTYPE html><html><head><title></title></head><body>{email.MessageBody}</body></html>"
            };

            var ordersInbox = MailboxAddress.Parse(username);
            var recipient = MailboxAddress.Parse(email.PrimaryEmailAddress);

            outboundMessage.From.Add(ordersInbox);
            outboundMessage.Cc.Add(ordersInbox);
            outboundMessage.To.Add(recipient);

            if (!string.IsNullOrWhiteSpace(email.SalesEmailAddress) && email.SalesEmailAddress.Contains('@'))
            {
                var sales = MailboxAddress.Parse(email.SalesEmailAddress);
                outboundMessage.Cc.Add(sales);
            }

            // If there's an attachment send it, if not just send the body.
            //if (Multipart != null && Multipart.Count > 0)
            //{
            //    builder.Attachments.Add(Multipart);
            //}

            if (!string.IsNullOrWhiteSpace(email.CalendarInvite))
            {
                var icsFile = Path.Combine(AppContext.BaseDirectory, "acceleratenetworks.ics");

                await System.IO.File.WriteAllTextAsync(icsFile, email.CalendarInvite);

                builder.Attachments.Add(icsFile);
            }

            outboundMessage.Body = builder.ToMessageBody();

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            smtp.MessageSent += (sender, args) => { };
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await smtp.ConnectAsync("mail.seattlemesh.net", 587, SecureSocketOptions.StartTls).ConfigureAwait(false);
            await smtp.AuthenticateAsync(username, password).ConfigureAwait(false);
            await smtp.SendAsync(outboundMessage).ConfigureAwait(false);
            await smtp.DisconnectAsync(true).ConfigureAwait(false);

            email.Completed = true;
            return true;
        }
        catch (Exception ex)
        {
            Log.Fatal($"[Email] Failed to send email {email.EmailId}.");
            Log.Fatal(ex.Message);
            Log.Fatal(ex.StackTrace ?? "StackTrace was null.");
            email.Completed = false;
            return false;
        }
    }
}