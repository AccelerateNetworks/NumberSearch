using Flurl.Http;

using Microsoft.EntityFrameworkCore;

using System.Text.Json.Serialization;

using Prometheus;
using System.ComponentModel.DataAnnotations;
using Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core
builder.Services.AddDbContext<MessagingContext>(opt => opt.UseSqlite());
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddMemoryCache();

var bulkVSUsername = builder.Configuration["BulkVSUsername"] ?? string.Empty;
var bulkVSPassword = builder.Configuration["BulkVSPassword"] ?? string.Empty;
var bulkVSInbound = builder.Configuration["BulkVSInboundMessagingURL"] ?? $"https://portal.bulkvs.com/api/v1.0/messageSend";
var teliToken = builder.Configuration["TeliToken"] ?? string.Empty;
var teliInbound = builder.Configuration["TeliInboundMessageSendingURL"] ?? $"https://api.teleapi.net/sms/send?token=";

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseHttpMetrics();

app.MapMetrics();

app.MapGet("/Conversations", async (string primary, MessagingContext db) =>
{
    var checkPrimary = PhoneNumbersNA.PhoneNumber.TryParse(primary, out var primaryNumber);
    if (!checkPrimary)
    {
        return Results.BadRequest("Primary number couldn't be parsed as a valid NANP (North American Number Plan) number.");
    }

    var messages = await db.Messages
                            .Where(x => x.ToFromCompound.Contains(primaryNumber.DialedNumber))
                            .OrderByDescending(x => x.DateRecievedUTC)
                            .ToListAsync();

    if (messages is not null && messages.Any())
    {
        var uniqueCompoundKeys = messages
                                    .DistinctBy(x => x.ToFromCompound)
                                    .OrderByDescending(x => x.DateRecievedUTC)
                                    .ToList();

        var recordsToRemove = new List<MessageRecord>();

        foreach (var record in uniqueCompoundKeys)
        {
            var reverseToAndFrom = $"{record.To},{record.From}";

            var reverseMatch = uniqueCompoundKeys.Where(x => x.ToFromCompound.Contains(reverseToAndFrom)).FirstOrDefault();

            if (reverseMatch is not null)
            {
                // Remove the older record.
                if (DateTime.Compare(reverseMatch.DateRecievedUTC, record.DateRecievedUTC) > 0)
                {
                    recordsToRemove.Add(record);
                }
                else
                {
                    recordsToRemove.Add(reverseMatch);
                }
            }
        }

        if (recordsToRemove.Any())
        {
            foreach (var item in recordsToRemove)
            {
                uniqueCompoundKeys.Remove(item);
            }
        }

        return Results.Ok(uniqueCompoundKeys);
    }
    else
    {
        return Results.NotFound();
    }
});

app.MapGet("/Thread", async (string primary, string contacts, MessagingContext db) =>
{
    var checkPrimary = PhoneNumbersNA.PhoneNumber.TryParse(primary, out var primaryNumber);
    if (!checkPrimary)
    {
        return Results.BadRequest("Primary number couldn't be parsed as a valid NANP (North American Number Plan) number.");
    }

    var contactNumbers = PhoneNumbersNA.Parse.AsPhoneNumbers(contacts).ToArray();

    if (contactNumbers is null || !contactNumbers.Any())
    {
        return Results.BadRequest("None of the contact numbers could be parsed as a valid NANP (North American Number Plan) number.");
    }

    var combined = new List<MessageRecord>();

    var outgoingCompoundKey = $"{primaryNumber.DialedNumber},{string.Join(',', contactNumbers.Select(x => x.DialedNumber))}";

    var outgoing = await db.Messages.Where(x => x.ToFromCompound == outgoingCompoundKey).ToListAsync();
    if (outgoing is not null && outgoing.Any())
    {
        combined.AddRange(outgoing);
    }

    var incomingCompoundKeys = new List<string>();

    foreach (var contact in contactNumbers)
    {
        incomingCompoundKeys.Add($"{contact.DialedNumber},{string.Join(',', contactNumbers.Append(primaryNumber).Where(x => x.DialedNumber != contact.DialedNumber).Select(x => x.DialedNumber))}");
    }

    foreach (var key in incomingCompoundKeys)
    {
        var incoming = await db.Messages.Where(x => x.ToFromCompound == key).ToListAsync();
        if (incoming is not null && incoming.Any())
        {
            combined.AddRange(incoming);
        }
    }

    if (combined.Any())
    {
        return Results.Ok(combined.OrderByDescending(x => x.DateRecievedUTC));
    }
    else
    {
        return Results.NotFound();
    }
});

// Listen for messages from the Teli Webhook.
app.MapPost("/Message/Inbound/Teli", async ([Microsoft.AspNetCore.Mvc.FromBody] TeliInbound message, MessagingContext db) =>
{
    // Validate and regularize the incoming message.
    if (!message.RegularizeAndValidate())
    {
        return Results.BadRequest("Phone Numbers could not be parsed as valid NANP (North American Number Plan) numbers.");
    }

    if (message is not null)
    {
        db.Messages.Add(message.ToMessageRecord());
        await db.SaveChangesAsync();

        // Typically we would give the 201 Created response here, but BulkVS expects a 200.
        return Results.Ok();
    }
    else
    {
        return Results.BadRequest();
    }
});

app.MapPost("/Message/Outbound/Teli", async ([Microsoft.AspNetCore.Mvc.FromBody] TeliOutbound message, MessagingContext db) =>
{
    // Validate and regularize the incoming message.
    if (!message.RegularizeAndValidate())
    {
        return Results.BadRequest("Phone Numbers could not be parsed as valid NANP (North American Numbering Plan) numbers.");
    }

    // SMS Messages are limited to 160 characters, however you can send up to 910 characters per Teli dev docs.
    // https://apidocs.teleapi.net/api/sms/sending-sms
    if (string.IsNullOrWhiteSpace(message.Message))
    {
        message.Message = message.Message.Length > 910
            ? message.Message[..910]
            : message.Message;
    }


    var sendMessage = await $"{teliInbound}{teliToken}"
                        .AllowAnyHttpStatus()
                        .PostJsonAsync(message)
                        .ReceiveJson<TeliOutboundError>();

    if (sendMessage is not null && sendMessage.IsSuccess())
    {
        var record = new MessageRecord
        {
            Id = Guid.NewGuid(),
            Content = message?.Message ?? string.Empty,
            DateRecievedUTC = DateTime.UtcNow,
            From = message?.FromPhoneNumber?.DialedNumber ?? string.Empty,
            To = string.Join(',', message?.ToPhoneNumbers?.Select(x => x.DialedNumber) ?? Array.Empty<string>()),
            MediaURLs = string.Empty,
            MessageSource = MessageSource.Outgoing,
            MessageType = message is not null && !string.IsNullOrWhiteSpace(message?.Message) && message?.Message?.Length > 160 ? MessageType.MMS : MessageType.SMS,
        };

        record.ToFromCompound = $"{record.From},{record.To}";

        db.Messages.Add(record);
        await db.SaveChangesAsync();

        return Results.Ok(new SendMessageResponse
        {
            MessageSent = true,
            Success = Array.Empty<string>(),
            Failure = Array.Empty<string>()
        });
    }
    else if (sendMessage is not null)
    {
        return Results.BadRequest($"Failed to submit message to Teli. {sendMessage?.Code} {sendMessage?.Status}: {sendMessage?.Data}");
    }

    return Results.BadRequest("Failed to submit message to Teli.");

});

// Listen for messages from the BulkVS Webhook.
app.MapPost("/Message/Inbound/BulkVS", async ([Microsoft.AspNetCore.Mvc.FromBody] BulkVSInbound message, MessagingContext db) =>
{
    // Validate and regularize the incoming message.
    if (!message.RegularizeAndValidate())
    {
        return Results.BadRequest("Phone Numbers could not be parsed as valid NANP (North American Number Plan) numbers.");
    }

    if (message is not null && message.MediaURLs is not null && message.MediaURLs.Any())
    {
        var validFileTypes = new List<string>();

        foreach (var link in message.MediaURLs)
        {
            if (link.EndsWith(".smil"))
            {
                validFileTypes.Add(link);
            }
            else if (link.EndsWith(".jpg"))
            {
                validFileTypes.Add(link);
            }
            else if (link.EndsWith(".jpeg"))
            {
                validFileTypes.Add(link);
            }
            else if (link.EndsWith(".txt"))
            {
                validFileTypes.Add(link);

                // In MMS messages, texts may be sent as attachments.
                if (string.IsNullOrWhiteSpace(message.Message))
                {
                    // TODO: Grab the contents from the text attachment and save them as the core message.
                    // Respect the 160 character limit.
                }
            }
        }

        message.MediaURLs = validFileTypes.ToArray();
    }

    if (message is not null)
    {
        db.Messages.Add(message.ToMessageRecord());
        await db.SaveChangesAsync();

        // Typically we would give the 201 Created response here, but BulkVS expects a 200.
        return Results.Ok();
    }
    else
    {
        return Results.BadRequest();
    }
});

app.MapPost("/Message/Outbound/BulkVS", async ([Microsoft.AspNetCore.Mvc.FromBody] BulkVSInbound message, MessagingContext db) =>
{
    // Validate and regularize the incoming message.
    if (!message.RegularizeAndValidate())
    {
        return Results.BadRequest("Phone Numbers could not be parsed as valid NANP (North American Numbering Plan) numbers.");
    }

    // (UPTO-160-CHARACTER-MESSAGE) per BulkVS dev docs.
    if (string.IsNullOrWhiteSpace(message.Message))
    {
        message.Message = message.Message.Length > 160
            ? message.Message[..160]
            : message.Message;
    }

    // Only two file types are valid for outgoing messages.
    if (message is not null && message.MediaURLs.Any())
    {
        var validFileTypes = new List<string>();

        foreach (var link in message.MediaURLs)
        {
            if (link.EndsWith(".png"))
            {
                validFileTypes.Add(link);
            }
            else if (link.EndsWith(".jpg"))
            {
                validFileTypes.Add(link);
            }
            else if (link.EndsWith(".jpeg"))
            {
                validFileTypes.Add(link);
            }
        }

        message.MediaURLs = validFileTypes.ToArray();
    }

    var sendMessage = await bulkVSInbound
                        .WithBasicAuth(bulkVSUsername, bulkVSPassword)
                        .PostJsonAsync(message)
                        .ReceiveJson<BulkVSOutboundResponse>();

    if (sendMessage is not null && sendMessage.Results is not null && sendMessage.Results.Any())
    {
        var success = new List<string>();
        var failure = new List<string>();

        foreach (var result in sendMessage.Results)
        {
            if (result.Status == "SUCCESS")
            {
                var checkNumber = PhoneNumbersNA.PhoneNumber.TryParse(result.To, out var parsedNumber);

                if (checkNumber)
                {
                    success.Add(parsedNumber.DialedNumber);
                }
                else
                {
                    if (result.To is not null)
                    {
                        success.Add(result.To);
                    }
                }
            }
            else
            {
                var checkNumber = PhoneNumbersNA.PhoneNumber.TryParse(result.To, out var parsedNumber);

                if (checkNumber)
                {
                    failure.Add(parsedNumber.DialedNumber);
                }
                else
                {
                    if (result.To is not null)
                    {
                        failure.Add(result.To);
                    }
                }
            }
        }

        var record = new MessageRecord
        {
            Id = Guid.NewGuid(),
            Content = message?.Message ?? string.Empty,
            DateRecievedUTC = DateTime.UtcNow,
            From = message?.FromPhoneNumber?.DialedNumber ?? string.Empty,
            To = string.Join(',', message?.ToPhoneNumbers?.Select(x => x.DialedNumber) ?? Array.Empty<string>()),
            MediaURLs = string.Join(',', message?.MediaURLs ?? Array.Empty<string>()),
            MessageSource = MessageSource.Outgoing,
            MessageType = message is not null && message.MediaURLs is not null && message.MediaURLs.Any() ? MessageType.MMS : MessageType.SMS,
        };

        record.ToFromCompound = $"{record.From},{record.To}";

        db.Messages.Add(record);
        await db.SaveChangesAsync();

        // Let the caller know that delivery status for specific numbers.
        return Results.Ok(new SendMessageResponse
        {
            MessageSent = true,
            Success = success.ToArray(),
            Failure = failure.ToArray()
        });
    }

    return Results.BadRequest("Failed to submit message to BulkVS.");
});

app.Run();


namespace Models
{
    // Message format supplied by BulkVS for both SMS and MMS.
    public class BulkVSInbound
    {
        public string? From { get; set; }
        public string[]? To { get; set; }
        public string? Message { get; set; }
        // Only used in MMS messages.
        public string[]? MediaURLs { get; set; }
        // These are for the regularization of phone numbers and not mapped from the JSON payload.
        [JsonIgnore]
        public PhoneNumbersNA.PhoneNumber? FromPhoneNumber { get; set; }
        [JsonIgnore]
        public List<PhoneNumbersNA.PhoneNumber>? ToPhoneNumbers { get; set; }

        // Convert from the vendor specific format to the generic format stored in the database.
        public MessageRecord ToMessageRecord()
        {
            var record = new MessageRecord
            {
                Id = Guid.NewGuid(),
                From = string.IsNullOrWhiteSpace(FromPhoneNumber?.DialedNumber) ? From : FromPhoneNumber?.DialedNumber,
                To = ToPhoneNumbers is not null && ToPhoneNumbers.Any() ? string.Join(',', ToPhoneNumbers.Select(x => x.DialedNumber)) : string.Join(',', To ?? Array.Empty<string>()),
                Content = Message,
                MediaURLs = MediaURLs is not null ? string.Join(',', MediaURLs) : string.Empty,
                MessageType = MediaURLs is not null && MediaURLs.Any() ? MessageType.MMS : MessageType.SMS,
                MessageSource = MessageSource.Incoming,
                DateRecievedUTC = DateTime.UtcNow
            };

            record.ToFromCompound = $"{record.From},{record.To}";

            return record;
        }

        public bool RegularizeAndValidate()
        {
            bool FromParsed = false;
            bool ToParsed = false;

            var checkFrom = PhoneNumbersNA.PhoneNumber.TryParse(From, out var fromPhoneNumber);
            if (checkFrom)
            {
                FromPhoneNumber = fromPhoneNumber;
                From = fromPhoneNumber.DialedNumber;
                FromParsed = true;
            }

            if (To is not null && To.Any())
            {
                // This may not be nessesary if this list is always created by the BulkVSMessage constructor.
                if (ToPhoneNumbers is null)
                {
                    ToPhoneNumbers = new List<PhoneNumbersNA.PhoneNumber>();
                }

                foreach (var number in To)
                {
                    var checkTo = PhoneNumbersNA.PhoneNumber.TryParse(number, out var toPhoneNumber);

                    if (checkTo)
                    {
                        ToPhoneNumbers.Add(toPhoneNumber);
                    }
                }

                // This will drop the numbers that couldn't be parsed.
                To = ToPhoneNumbers.Select(x => x.DialedNumber).ToArray();
                ToParsed = true;
            }

            return FromParsed && ToParsed;
        }
    }

    public class SendMessageResponse
    {
        public bool MessageSent { get; set; }
        public string[]? Success { get; set; }
        public string[]? Failure { get; set; }
    }

    public class BulkVSOutboundResponse
    {
        public string? RefId { get; set; }
        public string? From { get; set; }
        public string? MessageType { get; set; }
        public BulkVSOutboundResult[]? Results { get; set; }

        public class BulkVSOutboundResult
        {
            public string? To { get; set; }
            public string? Status { get; set; }
        }
    }

    public class TeliInbound
    {
        [JsonPropertyName("source")]
        public string? Source { get; set; }
        [JsonPropertyName("destination")]
        public string? Destination { get; set; }
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        // These are for the regularization of phone numbers and not mapped from the JSON payload.
        [JsonIgnore]
        public PhoneNumbersNA.PhoneNumber? FromPhoneNumber { get; set; }
        [JsonIgnore]
        public List<PhoneNumbersNA.PhoneNumber>? ToPhoneNumbers { get; set; }

        public bool RegularizeAndValidate()
        {
            bool FromParsed = false;
            bool ToParsed = false;

            var checkFrom = PhoneNumbersNA.PhoneNumber.TryParse(Source, out var fromPhoneNumber);
            if (checkFrom)
            {
                FromPhoneNumber = fromPhoneNumber;
                Source = fromPhoneNumber.DialedNumber;
                FromParsed = true;
            }

            var To = PhoneNumbersNA.Parse.AsDialedNumbers(Destination);

            if (To.Any())
            {
                // This may not be nessesary if this list is always created by the BulkVSMessage constructor.
                if (ToPhoneNumbers is null)
                {
                    ToPhoneNumbers = new List<PhoneNumbersNA.PhoneNumber>();
                }

                foreach (var number in To)
                {
                    var checkTo = PhoneNumbersNA.PhoneNumber.TryParse(number, out var toPhoneNumber);

                    if (checkTo)
                    {
                        ToPhoneNumbers.Add(toPhoneNumber);
                    }
                }

                // This will drop the numbers that couldn't be parsed.
                Destination = string.Join(',', ToPhoneNumbers.Select(x => x.DialedNumber));
                ToParsed = true;
            }

            return FromParsed && ToParsed;
        }

        public MessageRecord ToMessageRecord()
        {
            var record = new MessageRecord
            {
                Id = Guid.NewGuid(),
                From = string.IsNullOrWhiteSpace(FromPhoneNumber?.DialedNumber) ? Source : FromPhoneNumber?.DialedNumber,
                To = ToPhoneNumbers is not null && ToPhoneNumbers.Any() ? string.Join(',', ToPhoneNumbers.Select(x => x.DialedNumber)) : string.Join(',', Destination),
                Content = Message,
                MediaURLs = string.Empty,
                MessageType = string.IsNullOrWhiteSpace(Type) && Type == "mms" ? MessageType.MMS : MessageType.SMS,
                MessageSource = MessageSource.Incoming,
                DateRecievedUTC = DateTime.UtcNow
            };

            record.ToFromCompound = $"{record.From},{record.To}";

            return record;
        }
    }

    public class TeliOutbound
    {
        [JsonPropertyName("source")]
        public string? Source { get; set; }
        [JsonPropertyName("destination")]
        public string? Destination { get; set; }
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        // These are for the regularization of phone numbers and not mapped from the JSON payload.
        [JsonIgnore]
        public PhoneNumbersNA.PhoneNumber? FromPhoneNumber { get; set; }
        [JsonIgnore]
        public List<PhoneNumbersNA.PhoneNumber>? ToPhoneNumbers { get; set; }

        public bool RegularizeAndValidate()
        {
            bool FromParsed = false;
            bool ToParsed = false;

            var checkFrom = PhoneNumbersNA.PhoneNumber.TryParse(Source, out var fromPhoneNumber);
            if (checkFrom)
            {
                FromPhoneNumber = fromPhoneNumber;
                Source = fromPhoneNumber.DialedNumber;
                FromParsed = true;
            }

            var To = PhoneNumbersNA.Parse.AsDialedNumbers(Destination);

            if (To.Any())
            {
                // This may not be nessesary if this list is always created by the BulkVSMessage constructor.
                if (ToPhoneNumbers is null)
                {
                    ToPhoneNumbers = new List<PhoneNumbersNA.PhoneNumber>();
                }

                foreach (var number in To)
                {
                    var checkTo = PhoneNumbersNA.PhoneNumber.TryParse(number, out var toPhoneNumber);

                    if (checkTo)
                    {
                        ToPhoneNumbers.Add(toPhoneNumber);
                    }
                }

                // This will drop the numbers that couldn't be parsed.
                Destination = string.Join(',', ToPhoneNumbers.Select(x => x.DialedNumber));
                ToParsed = true;
            }

            return FromParsed && ToParsed;
        }
    }

    public class TeliOutboundError
    {
        [JsonPropertyName("code")]
        public int? Code { get; set; }
        [JsonPropertyName("status")]
        public string? Status { get; set; }
        [JsonPropertyName("data")]
        public string? Data { get; set; }

        public bool IsSuccess()
        {
            return Status is not null && Status == "success" && Code is not null && Code == 200;
        }
    }

    // Format maintained in the database.
    public class MessageRecord
    {
        [Key]
        public Guid Id { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public string? ToFromCompound { get; set; }
        public string? Content { get; set; }
        public string? MediaURLs { get; set; }
        public MessageType MessageType { get; set; }
        public MessageSource MessageSource { get; set; }
        // Convert to DateTimeOffset if db is not sqlite.
        public DateTime DateRecievedUTC { get; set; }
    }

    public enum MessageType { SMS, MMS };
    public enum MessageSource { Incoming, Outgoing };

    public class MessagingContext : DbContext
    {
        public MessagingContext(DbContextOptions<MessagingContext> options) : base(options)
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "Messaging.db");
        }

        public DbSet<MessageRecord> Messages => Set<MessageRecord>();
        public string DbPath { get; set; }

        // The following configures EF to create a Sqlite database file in the
        // special "local" folder for your platform.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath};Cache=Shared");
    }
}