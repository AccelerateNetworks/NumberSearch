using MailKit.Security;

using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;

using MimeKit;

using System.Threading.Tasks;

namespace NumberSearch.Ops.Services
{
    public class EmailSender(IConfiguration configuration) : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var outboundMessage = new MimeKit.MimeMessage
            {
                Sender = new MimeKit.MailboxAddress("Number Search", configuration.GetConnectionString("SmtpUsername")),
                Subject = subject
            };

            var builder = new BodyBuilder
            {
                HtmlBody = @$"<!DOCTYPE html><html><head><title></title></head><body><p>{htmlMessage}<p></body></html>"
            };

            var ordersInbox = MailboxAddress.Parse(configuration.GetConnectionString("SmtpUsername"));
            var recipient = MailboxAddress.Parse(email);


            outboundMessage.From.Add(ordersInbox);
            outboundMessage.Cc.Add(ordersInbox);
            outboundMessage.To.Add(recipient);

            outboundMessage.Body = builder.ToMessageBody();

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            smtp.MessageSent += (sender, args) => { };
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await smtp.ConnectAsync("mail.seattlemesh.net", 587, SecureSocketOptions.StartTls).ConfigureAwait(false);
            await smtp.AuthenticateAsync(configuration.GetConnectionString("SmtpUsername"), configuration.GetConnectionString("SmtpPassword")).ConfigureAwait(false);
            await smtp.SendAsync(outboundMessage).ConfigureAwait(false);
            await smtp.DisconnectAsync(true).ConfigureAwait(false);
        }
    }
}
