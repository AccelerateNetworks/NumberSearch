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
            string smtpUsername = configuration.GetConnectionString("SmtpUsername")
                ?? throw new System.InvalidOperationException("ConnectionStrings:SmtpUsername is not configured.");
            string smtpPassword = configuration.GetConnectionString("SmtpPassword")
                ?? throw new System.InvalidOperationException("ConnectionStrings:SmtpPassword is not configured.");

            var outboundMessage = new MimeKit.MimeMessage
            {
                Sender = new MimeKit.MailboxAddress("Number Search", smtpUsername),
                Subject = subject
            };

            var builder = new BodyBuilder
            {
                HtmlBody = @$"<!DOCTYPE html><html><head><title></title></head><body><p>{htmlMessage}<p></body></html>"
            };

            var ordersInbox = MailboxAddress.Parse(smtpUsername);
            var recipient = MailboxAddress.Parse(email);


            outboundMessage.From.Add(ordersInbox);
            outboundMessage.Cc.Add(ordersInbox);
            outboundMessage.To.Add(recipient);

            outboundMessage.Body = builder.ToMessageBody();

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            smtp.MessageSent += (sender, args) => { };

            await smtp.ConnectAsync("witcher.mxrouting.net", 587, SecureSocketOptions.StartTls).ConfigureAwait(false);
            await smtp.AuthenticateAsync(smtpUsername, smtpPassword).ConfigureAwait(false);
            await smtp.SendAsync(outboundMessage).ConfigureAwait(false);
            await smtp.DisconnectAsync(true).ConfigureAwait(false);
        }
    }
}
