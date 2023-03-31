using Flurl.Http;

using Messaging;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Models;

using Prometheus;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Microsoft.Extensions.Primitives;

Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                                    .Enrich.FromLogContext()
                                    .WriteTo.Console()
                .WriteTo.File(
                    $"{DateTime.Now:yyyyMMdd}_NumberSearch.Messaging.txt",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true
                )
                .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Configuration.AddUserSecrets("328593cf-cbb9-48e9-8938-e38a44c8291d");

    var bulkVSUsername = builder.Configuration.GetConnectionString("BulkVSUsername") ?? string.Empty;
    var bulkVSPassword = builder.Configuration.GetConnectionString("BulkVSPassword") ?? string.Empty;
    var bulkVSInbound = string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("BulkVSInboundMessagingURL")) ? $"https://portal.bulkvs.com/api/v1.0/messageSend" : builder.Configuration.GetConnectionString("BulkVSInboundMessagingURL");
    var teliToken = builder.Configuration.GetConnectionString("TeleAPI") ?? string.Empty;
    var teliInbound = string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("TeliInboundMessageSendingURL")) ? $"https://api.teleapi.net/sms/send?token=" : builder.Configuration.GetConnectionString("TeliInboundMessageSendingURL");
    var firstPointSMSOutbound = string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("FirstPointOutboundMessageURL")) ? $"https://smsapi.1pcom.net/v1/retailsendmessage" : builder.Configuration.GetConnectionString("FirstPointOutboundMessageURL");
    var firstPointUsername = builder.Configuration.GetConnectionString("PComNetUsername") ?? string.Empty;
    var firstPointPassword = builder.Configuration.GetConnectionString("PComNetPassword") ?? string.Empty;
    var localSecret = builder.Configuration.GetConnectionString("MessagingAPISecret") ?? string.Empty;

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ClockSkew = TimeSpan.Zero,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "AccelerateNetworks.Messaging",
                ValidAudience = "AccelerateNetworks.Messaging",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(localSecret)
                ),
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddCors();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(option =>
    {
        option.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1",
            Title = "AccelerateNetworks.Messaging",
            Description = "This API abstracts the sending and recieving of SMS/MMS messages to and from our upstream vendors.",
            Contact = new OpenApiContact
            {
                Name = string.Empty,
                Email = "dan@acceleratenetworks.com",
                Url = new Uri("https://acceleratenetworks.com/"),
            },
            License = new OpenApiLicense
            {
                Name = "Use under LICX",
                Url = new Uri("https://github.com/AccelerateNetworks/NumberSearch/blob/master/LICENSE"),
            }
        });
        option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter a valid token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        });
        option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

        // Set the comments path for the Swagger JSON and UI.
        //var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        //var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        //option.IncludeXmlComments(xmlPath);
    });

    // EF Core
    builder.Services.AddDbContext<MessagingContext>(opt => opt.UseSqlite());
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(
                        builder.Configuration.GetConnectionString("PostgresqlProd")));

    builder.Services.AddIdentityCore<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>();

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });

    builder.Services.AddRateLimiter(_ => _
        .AddFixedWindowLimiter(policyName: "onePerSecond", options =>
        {
            options.PermitLimit = 1;
            options.Window = TimeSpan.FromSeconds(1);
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 2;
        }));

    builder.Services.AddMemoryCache();
    builder.Services.AddScoped<TokenService, TokenService>();

    var app = builder.Build();

    // Create the database if it doesn't exist
    var contextOptions = new DbContextOptionsBuilder<MessagingContext>()
        .UseSqlite()
        .Options;
    using var dbContext = new MessagingContext(contextOptions);
    await dbContext.Database.MigrateAsync();

    app.UseCors();
    app.UseAuthentication();

    // Uncomment for debugging the request pipeline.
    //app.Use((context, next) =>
    //{
    //    // Put a breakpoint here
    //    return next(context);
    //});

    app.UseAuthorization();
    app.UseHttpsRedirection();
    app.UseSwagger();
    app.UseDeveloperExceptionPage();

    // Swagger defaults
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Messaging");
    });

    // Set the app root to the swagger docs
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Messaging");
        c.RoutePrefix = string.Empty;
    });

    app.UseSecurityHeaders();
    app.UseHttpMetrics();

    app.MapMetrics();

    app.MapPost("/login", async (AuthRequest request, ApplicationDbContext db, UserManager<IdentityUser> userManager, TokenService tokenService, IConfiguration configuration) =>
    {
        var managedUser = await userManager.FindByEmailAsync(request.Email);
        if (managedUser is null)
        {
            return Results.BadRequest("Bad credentials");
        }
        var isPasswordValid = await userManager.CheckPasswordAsync(managedUser, request.Password);
        if (!isPasswordValid)
        {
            return Results.BadRequest("Bad credentials");
        }
        var userInDb = db.Users.FirstOrDefault(u => u.Email == request.Email);
        if (userInDb is null)
            return Results.Unauthorized();
        var accessToken = tokenService.CreateToken(userInDb, configuration);
        await db.SaveChangesAsync();
        return Results.Ok(new AuthResponse
        {
            Username = userInDb?.UserName ?? string.Empty,
            Email = userInDb?.Email ?? string.Empty,
            Token = accessToken,
        });
    }).RequireRateLimiting("onePerSecond");

    app.MapGet("/conversations", async (string primary, MessagingContext db, ClaimsPrincipal user) =>
    {
        var checkPrimary = PhoneNumbersNA.PhoneNumber.TryParse(primary, out var primaryNumber);
        if (!checkPrimary)
        {
            return Results.BadRequest("Primary number couldn't be parsed as a valid NANP (North American Number Plan) number.");
        }

        if (primaryNumber is not null && !string.IsNullOrWhiteSpace(primaryNumber.DialedNumber))
        {
            var messages = await db.Messages
                            .Where(x => x.ToFromCompound.Contains(primaryNumber.DialedNumber))
                            .OrderByDescending(x => x.DateReceivedUTC)
                            .ToListAsync();

            if (messages is not null && messages.Any())
            {
                var uniqueCompoundKeys = messages
                                            .DistinctBy(x => x.ToFromCompound)
                                            .OrderByDescending(x => x.DateReceivedUTC)
                                            .ToList();

                var recordsToRemove = new List<MessageRecord>();

                foreach (var record in uniqueCompoundKeys)
                {
                    var reverseToAndFrom = $"{record.To},{record.From}";

                    var reverseMatch = uniqueCompoundKeys.Where(x => x.ToFromCompound?.Contains(reverseToAndFrom) ?? false).FirstOrDefault();

                    if (reverseMatch is not null)
                    {
                        // Remove the older record.
                        if (DateTime.Compare(reverseMatch.DateReceivedUTC, record.DateReceivedUTC) > 0)
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

        }

        return Results.NotFound();
    })
        .RequireAuthorization();

    app.MapGet("/thread", async (string primary, string contacts, MessagingContext db) =>
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

        var outgoingCompoundKey = $"{primaryNumber?.DialedNumber},{string.Join(',', contactNumbers.Select(x => x.DialedNumber))}";

        var outgoing = await db.Messages.Where(x => x.ToFromCompound == outgoingCompoundKey).ToListAsync();
        if (outgoing is not null && outgoing.Any())
        {
            combined.AddRange(outgoing);
        }

        var incomingCompoundKeys = new List<string>();

        foreach (var contact in contactNumbers)
        {
            incomingCompoundKeys.Add($"{contact.DialedNumber},{string.Join(',', contactNumbers.Append(primaryNumber).Where(x => x!.DialedNumber != contact.DialedNumber).Select(x => x!.DialedNumber))}");
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
            return Results.Ok(combined.OrderByDescending(x => x.DateReceivedUTC));
        }
        else
        {
            return Results.NotFound();
        }
    }).RequireAuthorization();

    app.MapPost("/api/inbound/1pcom", async (HttpContext ctx, string token, MessagingContext db) =>
    {
        if (token is not "okereeduePeiquah3yaemohGhae0ie")
        {
            Log.Warning($"Token is not valid. Token: {token} is not okereeduePeiquah3yaemohGhae0ie");
        }

        try
        {
            FirstPointInbound message = new()
            {
                msisdn = ctx.Request.Form["msisdn"].ToString(),
                to = ctx.Request.Form["to"].ToString(),
                message = ctx.Request.Form["message"].ToString(),
                sessionid = ctx.Request.Form["sessionid"].ToString(),
                serversecret = ctx.Request.Form["serversecret"].ToString(),
                timezone = ctx.Request.Form["timezone"].ToString(),
                origtime = ctx.Request.Form["origtime"].ToString(),
            };

            // Validate and regularize the incoming message.
            if (!message.RegularizeAndValidate())
            {
                return Results.BadRequest($"Phone Numbers could not be parsed as valid NANP (North American Numbering Plan) numbers. {System.Text.Json.JsonSerializer.Serialize(message)}");
            }

            MessageRecord record = new()
            {
                Content = message.message,
                From = message.FromPhoneNumber.DialedNumber,
                To = string.Join(',', message?.ToPhoneNumbers?.Select(x => x.DialedNumber) ?? Array.Empty<string>()),
                MessageSource = MessageSource.Incoming,
                MessageType = MessageType.SMS,
                MediaURLs = string.Empty,
            };

            record.ToFromCompound = $"{record.From},{record.To}";
            db.Messages.Add(record);

            try
            {
                await db.SaveChangesAsync();

                var existingRegistration = await db.ClientRegistrations.Where(x => x.AsDialed == record.To).FirstOrDefaultAsync();
                if (existingRegistration is not null && existingRegistration.AsDialed == record.To)
                {
                    try
                    {
                        _ = await existingRegistration.CallbackUrl.PostJsonAsync(record);
                        return Results.Ok("The incoming message was recieved and forwarded to the client.");
                    }
                    catch (FlurlHttpException ex)
                    {
                        Log.Error(await ex.GetResponseStringAsync());
                        return Results.Problem("Failed to forward the message to the client's callback url.");
                    }
                }
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to save incoming message to the database. {ex.Message}");
            }

        }
        catch (Exception ex)
        {
            Log.Information("Failed to read form data by field name.");
        }

        return Results.Ok("Message recieved.");
    });

    app.MapPost("/registerclient", async (string asDialed, string callBackUrl, MessagingContext db) =>
    {
        var checkAsDialed = PhoneNumbersNA.PhoneNumber.TryParse(asDialed, out var asDialedNumber);
        if (!checkAsDialed)
        {
            return Results.BadRequest("As Dialed number couldn't be parsed as a valid NANP (North American Number Plan) number.");
        }

        // Validate the callback Url to prevent dumb errors.
        if (!Uri.IsWellFormedUriString(callBackUrl, UriKind.Absolute))
        {
            return Results.BadRequest($"The callback Url provided {callBackUrl} is invalid or not a well formatted Uri. Please read https://learn.microsoft.com/en-us/dotnet/api/system.uri.iswellformeduristring?view=net-7.0 for more information.");
        }

        try
        {
            // Update existing registrations before creating new ones.
            var existingRegistration = await db.ClientRegistrations.Where(x => x.AsDialed == asDialedNumber.DialedNumber).FirstOrDefaultAsync();

            if (existingRegistration is not null && !string.IsNullOrWhiteSpace(existingRegistration.CallbackUrl))
            {
                existingRegistration.CallbackUrl = callBackUrl;
            }
            else
            {
                await db.AddAsync(new ClientRegistration
                {
                    AsDialed = asDialedNumber.DialedNumber,
                    CallbackUrl = callBackUrl,
                });
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.StackTrace ?? "No stacktrace found.");
            return Results.Problem("Failed to save the registration to the database.");
        }

        return Results.Ok($"{asDialedNumber.DialedNumber} is now registered to {callBackUrl}");

    }).RequireAuthorization();

    // This only works for errors, we need functional credentials to finish building it out.
    app.MapPost("/message/send", async ([Microsoft.AspNetCore.Mvc.FromBody] FirstPointOutbound message, MessagingContext db) =>
    {
        // https://portal.1pcom.net/download/SMSAPI.pdf
        // Validate and regularize the incoming message.
        if (!message.RegularizeAndValidate())
        {
            return Results.BadRequest("Phone Numbers could not be parsed as valid NANP (North American Numbering Plan) numbers.");
        }

        try
        {
            var sendMessage = await firstPointSMSOutbound
                    .PostUrlEncodedAsync(new
                    {
                        username = firstPointUsername,
                        password = firstPointPassword,
                        to = message.To,
                        msisdn = message.MSISDN,
                        messagebody = message.Message
                    })
                    .ReceiveJson<FirstPointResponse>();

            if (sendMessage is not null && sendMessage?.Response?.Text is "OK")
            {
                var record = new MessageRecord
                {
                    Id = Guid.NewGuid(),
                    Content = message?.Message ?? string.Empty,
                    DateReceivedUTC = DateTime.UtcNow,
                    From = message?.FromPhoneNumber?.DialedNumber ?? string.Empty,
                    To = string.Join(',', message?.ToPhoneNumbers?.Select(x => x.DialedNumber) ?? Array.Empty<string>()),
                    MediaURLs = string.Empty,
                    MessageSource = MessageSource.Outgoing,
                    MessageType = MessageType.SMS,
                    DLRID = sendMessage.Response.DLRID
                };

                record.ToFromCompound = $"{record.From},{record.To}";

                db.Messages.Add(record);
                await db.SaveChangesAsync();

                // Let the caller know that delivery status for specific numbers.
                return Results.Ok(new SendMessageResponse
                {
                    MessageSent = true,
                });
            }
            else
            {
                return Results.BadRequest($"Failed to submit message to FirstPoint. {System.Text.Json.JsonSerializer.Serialize(sendMessage)}");
            }
        }
        catch (FlurlHttpException ex)
        {
            var error = await ex.GetResponseJsonAsync<FirstPointResponse>();

            // Let the caller know that delivery status for specific numbers.
            return Results.BadRequest($"Failed to submit message to FirstPoint. {System.Text.Json.JsonSerializer.Serialize(error)}");
        }

    }).RequireAuthorization();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

namespace Models
{
    // Example Response JSON
    //    {
    //    "response": {
    //        "code": 0,
    //        "developertext": "",
    //        "dlrid": "78261715",
    //        "subcode": 0,
    //        "text": "OK"
    //      }
    //    }
    public class FirstPointResponse
    {
        [JsonPropertyName("response")]
        public Response Response { get; set; } = new();
    }
    public class Response
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("developertext")]
        public string DeveloperText { get; set; } = string.Empty;
        [JsonPropertyName("dlrid")]
        public string DLRID { get; set; } = string.Empty;
        [JsonPropertyName("subcode")]
        public int Subcode { get; set; }
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    // This model isn't converted into JSON as First Com expects a form-style URLEncoded POST as a request. Only the response is actually JSON.
    public class FirstPointOutbound
    {
        public string To { get; set; } = string.Empty;
        public string MSISDN { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        // These are for the regularization of phone numbers and not mapped from the JSON payload.
        [JsonIgnore]
        public PhoneNumbersNA.PhoneNumber FromPhoneNumber { get; set; } = new();
        [JsonIgnore]
        public List<PhoneNumbersNA.PhoneNumber> ToPhoneNumbers { get; set; } = new();

        public bool RegularizeAndValidate()
        {
            bool FromParsed = false;
            bool ToParsed = false;

            if (!string.IsNullOrWhiteSpace(MSISDN))
            {
                var checkFrom = PhoneNumbersNA.PhoneNumber.TryParse(MSISDN, out var fromPhoneNumber);
                if (checkFrom && fromPhoneNumber is not null && !string.IsNullOrWhiteSpace(fromPhoneNumber.DialedNumber))
                {
                    FromPhoneNumber = fromPhoneNumber;
                    MSISDN = $"1{fromPhoneNumber.DialedNumber}";
                    FromParsed = true;
                }
            }

            if (To is not null && To.Any())
            {
                // This may not be necessary if this list is always created by the BulkVSMessage constructor.
                ToPhoneNumbers ??= new List<PhoneNumbersNA.PhoneNumber>();

                foreach (var number in To.Split(','))
                {
                    var checkTo = PhoneNumbersNA.PhoneNumber.TryParse(number, out var toPhoneNumber);

                    if (checkTo && toPhoneNumber is not null)
                    {
                        ToPhoneNumbers.Add(toPhoneNumber);
                    }
                }

                // This will drop the numbers that couldn't be parsed.
                To = ToPhoneNumbers is not null && ToPhoneNumbers.Any() ? ToPhoneNumbers.Count > 1 ? string.Join(",", ToPhoneNumbers.Select(x => $"1{x.DialedNumber!}")) : $"1{ToPhoneNumbers?.FirstOrDefault()?.DialedNumber}" ?? string.Empty : string.Empty;
                ToParsed = true;
            }

            return FromParsed && ToParsed;
        }

    }

    public class FirstPointInbound
    {
        public string origtime { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string to { get; set; } = string.Empty;
        public string sessionid { get; set; } = string.Empty;
        public string timezone { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        // We don't care so we're not going to serialize this field.
        //public float api_version { get; set; }
        public string serversecret { get; set; } = string.Empty;
        // These are for the regularization of phone numbers and not mapped from the JSON payload.
        [JsonIgnore]
        public PhoneNumbersNA.PhoneNumber FromPhoneNumber { get; set; } = new();
        [JsonIgnore]
        public List<PhoneNumbersNA.PhoneNumber> ToPhoneNumbers { get; set; } = new();

        public bool RegularizeAndValidate()
        {
            bool FromParsed = false;
            bool ToParsed = false;

            if (!string.IsNullOrWhiteSpace(msisdn))
            {
                var checkFrom = PhoneNumbersNA.PhoneNumber.TryParse(msisdn, out var fromPhoneNumber);
                if (checkFrom && fromPhoneNumber is not null && !string.IsNullOrWhiteSpace(fromPhoneNumber.DialedNumber))
                {
                    FromPhoneNumber = fromPhoneNumber;
                    msisdn = $"1{fromPhoneNumber.DialedNumber}";
                    FromParsed = true;
                }
            }

            if (to is not null && to.Any())
            {
                // This may not be necessary if this list is always created by the BulkVSMessage constructor.
                ToPhoneNumbers ??= new List<PhoneNumbersNA.PhoneNumber>();

                foreach (var number in to.Split(','))
                {
                    var checkTo = PhoneNumbersNA.PhoneNumber.TryParse(number, out var toPhoneNumber);

                    if (checkTo && toPhoneNumber is not null)
                    {
                        ToPhoneNumbers.Add(toPhoneNumber);
                    }
                }

                // This will drop the numbers that couldn't be parsed.
                to = ToPhoneNumbers is not null && ToPhoneNumbers.Any() ? ToPhoneNumbers.Count > 1 ? string.Join(",", ToPhoneNumbers.Select(x => $"1{x.DialedNumber!}")) : $"1{ToPhoneNumbers?.FirstOrDefault()?.DialedNumber}" ?? string.Empty : string.Empty;
                ToParsed = true;
            }

            return FromParsed && ToParsed;
        }
    }

    public class SendMessageResponse
    {
        public bool MessageSent { get; set; }
        public string[] Success { get; set; } = Array.Empty<string>();
        public string[] Failure { get; set; } = Array.Empty<string>();
    }

    // Format maintained in the database.
    public class MessageRecord
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string ToFromCompound { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string MediaURLs { get; set; } = string.Empty;
        public MessageType MessageType { get; set; }
        public MessageSource MessageSource { get; set; }
        // Convert to DateTimeOffset if db is not SQLite.
        public DateTime DateReceivedUTC { get; set; } = DateTime.UtcNow;
        public string DLRID { get; set; } = string.Empty;
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
        public DbSet<ClientRegistration> ClientRegistrations => Set<ClientRegistration>();

        public string DbPath { get; set; }

        // The following configures EF to create a SQLite database file in the
        // special "local" folder for your platform.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath};Cache=Shared");
    }

    public class AuthRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class ClientRegistration
    {
        [Key]
        public Guid ClientRegistrationId { get; set; } = Guid.NewGuid();
        public string AsDialed { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
        public DateTime DateRegistered { get; set; } = DateTime.Now;
    }
}