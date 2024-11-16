using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using Models;

using System.Runtime.InteropServices;

namespace Messaging
{
    public class TimedHostedService(ILogger<TimedHostedService> logger, AppSettings appSettings) : BackgroundService
    {
        private readonly ILogger<TimedHostedService> _logger = logger;
        private int _executionCount;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[Background Worker] Timed Hosted Service running.");

            // When the timer should have no due-time, then do the work once now.
            await DoWork(appSettings, stoppingToken);

            // Most email clients refresh every 10 seconds per a brief googling.
            using PeriodicTimer timer = new(TimeSpan.FromSeconds(10));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await DoWork(appSettings, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[Background Worker] Timed Hosted Service is stopping.");
            }
        }

        // Could also be a async method, that can be awaited in ExecuteAsync above
        private async Task DoWork(AppSettings appSettings, CancellationToken cls)
        {
            int count = Interlocked.Increment(ref _executionCount);

            _logger.LogInformation("[Background Worker] Timed Hosted Service is running.");

            var contextOptions = new DbContextOptionsBuilder<MessagingContext>()
                                .UseSqlite()
                                .Options;
            using var dbContext = new MessagingContext(contextOptions);

            try
            {
                // Check for emails
                var emails = await EmailMessage.GetEmailsAsync(appSettings.ConnectionStrings.EmailUsername, appSettings.ConnectionStrings.EmailPassword, cls);
                // Transform each email to a text message
                foreach (var email in MemoryMarshal.ToEnumerable(emails))
                {
                    await EmailToForwardedMessageAsync(email, dbContext);
                    _logger.LogInformation("[Background Worker] [EmailToText] Forwarded message To {to} From {from}", email.To, email.From);
                }
            }
            catch (Exception ex) 
            {

                _logger.LogInformation("[Background Worker] [EmailToText] Failed to get new email messages. {ex}", ex);
            }

            // Send the messages outbound
            _logger.LogInformation("[Background Worker] Timed Hosted Service has completed.");
        }

        private async Task EmailToForwardedMessageAsync(EmailMessage.InboundEmail email, MessagingContext messagingContext)
        {
            var message = new SendMessageRequest()
            {
                // Reverse from and to here, as we are responding.
                To = email.From,
                MSISDN = email.To,
                MediaURLs = [],
                Message = email.Content
            };
            _logger.LogInformation("[Background Worker] Send Message Request: {message}", System.Text.Json.JsonSerializer.Serialize(message));
            var response = await Endpoints.SendMessageAsync(message, false, appSettings, messagingContext);
            var text = System.Text.Json.JsonSerializer.Serialize(response.Result as Ok<SendMessageResponse>);
            text = string.IsNullOrWhiteSpace(text) ? System.Text.Json.JsonSerializer.Serialize(response.Result as BadRequest<SendMessageResponse>) : text;
            _logger.LogInformation("[Background Worker] Send Message Response: {text}", text);
        }
    }
}
