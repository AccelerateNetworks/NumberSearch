using Amazon.S3;
using Amazon.S3.Transfer;

using Flurl.Http;

using Messaging;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Models;

using Prometheus;

using Serilog;
using Serilog.Events;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

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

    string bulkVSUsername = builder.Configuration.GetConnectionString("BulkVSUsername") ?? string.Empty;
    string bulkVSPassword = builder.Configuration.GetConnectionString("BulkVSPassword") ?? string.Empty;
    string bulkVSInbound = string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("BulkVSInboundMessagingURL"))
        ? @"https://portal.bulkvs.com/api/v1.0/messageSend" : builder.Configuration.GetConnectionString("BulkVSInboundMessagingURL") ?? string.Empty;
    string firstPointSMSOutbound = string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("FirstPointOutboundMessageURL"))
        ? @"https://smsapi.1pcom.net/v1/retailsendmessage" : builder.Configuration.GetConnectionString("FirstPointOutboundMessageURL") ?? string.Empty;
    string firstPointMMSOutbound = string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("FirstPointOutboundMMSMessageURL"))
        ? @"https://mmsc01.1pcom.net/MMS_Send" : builder.Configuration.GetConnectionString("FirstPointOutboundMMSMessageURL") ?? string.Empty;
    string firstPointMMSInbound = string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("FirstPointOutboundMMSMessageURL"))
        ? @"https://mmsc01.1pcom.net/MMS_Send" : builder.Configuration.GetConnectionString("FirstPointOutboundMMSMessageURL") ?? string.Empty;
    string firstPointUsername = builder.Configuration.GetConnectionString("PComNetUsername") ?? string.Empty;
    string firstPointPassword = builder.Configuration.GetConnectionString("PComNetPassword") ?? string.Empty;
    string localSecret = builder.Configuration.GetConnectionString("MessagingAPISecret") ?? string.Empty;
    string digitalOceanSpacesBucket = builder.Configuration.GetConnectionString("BucketName") ?? string.Empty;
    string digitalOceanSpacesAccessKey = builder.Configuration.GetConnectionString("DOSpacesAccessKey") ?? string.Empty;
    string digitalOceanSpacesSecretKey = builder.Configuration.GetConnectionString("DOSpacesSecretKey") ?? string.Empty;
    string s3ServiceURL = builder.Configuration.GetConnectionString("S3ServiceURL") ?? string.Empty;

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ClockSkew = TimeSpan.Zero,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "sms.callpipe.com",
                ValidAudience = "sms.callpipe.com",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(localSecret)
                ),
            };
        });

    // Makes enums show up in the docs correctly.
    builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
    {
        options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
    builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

    builder.Services.AddAuthorization();
    builder.Services.AddCors();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(option =>
    {
        option.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1",
            Title = "sms.callpipe.com",
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
        option.UseInlineDefinitionsForEnums();
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
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
    builder.Services.AddAWSService<IAmazonS3>();

    builder.Services.AddHttpLogging(httpLogging =>
     {
         httpLogging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
         httpLogging.RequestBodyLogLimit = 4096;
         httpLogging.ResponseBodyLogLimit = 4096;
     });

    var app = builder.Build();

    // Create the database if it doesn't exist
    var contextOptions = new DbContextOptionsBuilder<MessagingContext>()
        .UseSqlite()
        .Options;
    using var dbContext = new MessagingContext(contextOptions);
    await dbContext.Database.MigrateAsync();

    // Make sure the bucket exists.
    var spacesConfig = new AmazonS3Config
    {
        ServiceURL = "https://accelerate-networks-mms.sfo3.digitaloceanspaces.com"
    };
    var spacesClient = new AmazonS3Client(digitalOceanSpacesAccessKey, digitalOceanSpacesSecretKey, spacesConfig);
    _ = await spacesClient.PutBucketAsync(digitalOceanSpacesBucket);

    app.UseCors();
    app.UseAuthentication();
    app.UseHttpLogging();

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

    app.MapPost("/login", async Task<Results<Ok<AuthResponse>, BadRequest<string>, UnauthorizedHttpResult>> (AuthRequest request, ApplicationDbContext db, UserManager<IdentityUser> userManager, TokenService tokenService, IConfiguration configuration) =>
    {
        var managedUser = await userManager.FindByEmailAsync(request.Email);
        if (managedUser is null)
        {
            return TypedResults.BadRequest("Bad credentials");
        }
        var isPasswordValid = await userManager.CheckPasswordAsync(managedUser, request.Password);
        if (!isPasswordValid)
        {
            return TypedResults.BadRequest("Bad credentials");
        }
        var userInDb = db.Users.FirstOrDefault(u => u.Email == request.Email);
        if (userInDb is null)
            return TypedResults.Unauthorized();
        var accessToken = tokenService.CreateToken(userInDb, configuration);
        await db.SaveChangesAsync();
        return TypedResults.Ok(new AuthResponse
        {
            Username = userInDb?.UserName ?? string.Empty,
            Email = userInDb?.Email ?? string.Empty,
            Token = accessToken,
        });
    })
        .RequireRateLimiting("onePerSecond").WithOpenApi(x => new(x) { Summary = "Get an auth token.", Description = "JWT magic bb." });

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
        .RequireAuthorization().ExcludeFromDescription().WithOpenApi();

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
    }).RequireAuthorization().ExcludeFromDescription().WithOpenApi();

    app.MapGet("/client", async Task<Results<Ok<ClientRegistration>, BadRequest<string>, NotFound<string>>> (string asDialed, MessagingContext db) =>
    {
        var checkAsDialed = PhoneNumbersNA.PhoneNumber.TryParse(asDialed, out var asDialedNumber);
        if (!checkAsDialed)
        {
            return TypedResults.BadRequest("AsDialed number couldn't be parsed as a valid NANP (North American Number Plan) number.");
        }

        try
        {
            var registration = await db.ClientRegistrations.FirstOrDefaultAsync(x => x.AsDialed == asDialedNumber.DialedNumber);

            if (registration is not null && !string.IsNullOrWhiteSpace(registration.AsDialed) && registration.AsDialed == asDialedNumber.DialedNumber)
            {
                return TypedResults.Ok(registration);
            }
            else
            {
                return TypedResults.NotFound($"Failed to find a client registration for {asDialed}.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.StackTrace ?? "No stacktrace found.");
            return TypedResults.BadRequest(ex.Message);
        }

    })
        .RequireAuthorization().WithOpenApi(x => new(x) { Summary = "Lookup a specific client registration using the dialed number.", Description = "Use this to see if a dialed number is registered and find out what callback Url its registered to." });

    app.MapGet("/client/all", async Task<Results<Ok<ClientRegistration[]>, BadRequest<string>, NotFound<string>>> (bool? clear, MessagingContext db) =>
    {
        try
        {

            var registrations = await db.ClientRegistrations.ToArrayAsync();

            if (clear is not null && clear is true)
            {
                db.ClientRegistrations.RemoveRange(registrations);
                await db.SaveChangesAsync();
            }

            registrations = await db.ClientRegistrations.ToArrayAsync();

            if (registrations is not null && registrations.Any())
            {
                return TypedResults.Ok(registrations);
            }
            else
            {
                return TypedResults.NotFound($"No clients are currently registered for service.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.StackTrace ?? "No stacktrace found.");
            return TypedResults.BadRequest(ex.Message);
        }

    })
        .RequireAuthorization().WithOpenApi(x => new(x) { Summary = "View all registered clients.", Description = "This is intended to be used for debugging client registrations." });

    app.MapPost("/client/register", async Task<Results<Ok<RegistrationResponse>, BadRequest<RegistrationResponse>>> (RegistrationRequest registration, MessagingContext db) =>
    {
        var checkAsDialed = PhoneNumbersNA.PhoneNumber.TryParse(registration.DialedNumber, out var asDialedNumber);
        if (!checkAsDialed)
        {
            return TypedResults.BadRequest(new RegistrationResponse
            {
                DialedNumber = registration.DialedNumber,
                CallbackUrl = registration.CallbackUrl,
                Registered = false,
                Message = "Dialed number couldn't be parsed as a valid NANP (North American Number Plan) number."
            });
        }

        // Validate the callback Url to prevent dumb errors.
        if (!Uri.IsWellFormedUriString(registration.CallbackUrl, UriKind.Absolute))
        {
            return TypedResults.BadRequest(new RegistrationResponse
            {
                DialedNumber = registration.DialedNumber,
                CallbackUrl = registration.CallbackUrl,
                Registered = false,
                Message = $"The callback Url provided {registration.CallbackUrl} is invalid or not a well formatted Uri. Please read https://learn.microsoft.com/en-us/dotnet/api/system.uri.iswellformeduristring?view=net-7.0 for more information."
            });
        }

        try
        {
            // Update existing registrations before creating new ones.
            var existingRegistration = await db.ClientRegistrations.Where(x => x.AsDialed == asDialedNumber.DialedNumber).FirstOrDefaultAsync();

            if (existingRegistration is not null)
            {
                // If they really are different update the record.
                if (existingRegistration.CallbackUrl != registration.CallbackUrl || existingRegistration.ClientSecret != registration.ClientSecret)
                {
                    existingRegistration.CallbackUrl = registration.CallbackUrl;
                    existingRegistration.ClientSecret = registration.ClientSecret;
                    await db.SaveChangesAsync();
                }

                // Otherwise do nothing.
            }
            else
            {
                await db.AddAsync(new ClientRegistration
                {
                    AsDialed = asDialedNumber.DialedNumber,
                    CallbackUrl = registration.CallbackUrl,
                    ClientSecret = registration.ClientSecret,
                });
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.StackTrace ?? "No stacktrace found.");
            return TypedResults.BadRequest(new RegistrationResponse
            {
                DialedNumber = registration.DialedNumber,
                CallbackUrl = registration.CallbackUrl,
                Registered = false,
                Message = $"Failed to save the registration to the database. Please email dan@acceleratenetworks.com to report this outage. {ex.Message}"
            });
        }

        return TypedResults.Ok(new RegistrationResponse { DialedNumber = asDialedNumber.DialedNumber, CallbackUrl = registration.CallbackUrl, Registered = true });

    })
        .RequireAuthorization().WithOpenApi(x => new(x) { Summary = "Register a client for message forwarding.", Description = "Boy I wish I had more to say about this, lmao." });

    app.MapGet("/message/all", async Task<Results<Ok<MessageRecord[]>, NotFound<string>, BadRequest<string>>> (MessagingContext db) =>
    {
        try
        {
            var messages = await db.Messages.OrderByDescending(x => x.DateReceivedUTC).Take(10).ToArrayAsync();

            if (messages is not null && messages.Any())
            {
                return TypedResults.Ok(messages);
            }
            else
            {
                return TypedResults.NotFound($"No messages have been recorded.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.StackTrace ?? "No stacktrace found.");
            return TypedResults.BadRequest(ex.Message);
        }
    })
        .RequireAuthorization().WithOpenApi(x => new(x) { Summary = "View all sent and recieved messages.", Description = "This is intended to help you debug problems with message sending and delievery so you can see if it's this API or the upstream vendor that is causing problems." });

    app.MapPost("/message/send", async Task<Results<Ok<SendMessageResponse>, BadRequest<SendMessageResponse>>> ([Microsoft.AspNetCore.Mvc.FromBody] SendMessageRequest message, MessagingContext db) =>
    {
        // https://portal.1pcom.net/download/SMSAPI.pdf
        // Validate and regularize the incoming message.
        if (!message.RegularizeAndValidate())
        {
            return TypedResults.BadRequest(new SendMessageResponse { Message = "Phone Numbers could not be parsed as valid NANP (North American Numbering Plan) numbers." });
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
                    From = message?.MSISDN ?? "MSISDN was blank",
                    To = message?.To ?? "To was blank",
                    MediaURLs = string.Empty,
                    MessageSource = MessageSource.Outgoing,
                    MessageType = MessageType.SMS,
                    DLRID = sendMessage.Response.DLRID
                };

                record.ToFromCompound = $"{record.From},{record.To}";

                db.Messages.Add(record);
                await db.SaveChangesAsync();

                // Let the caller know that delivery status for specific numbers.
                return TypedResults.Ok(new SendMessageResponse
                {
                    Message = $"Message sent to {record.To}",
                    MessageSent = true,
                });
            }
            else
            {
                return TypedResults.BadRequest(new SendMessageResponse { Message = $"Failed to submit message to FirstPoint. {System.Text.Json.JsonSerializer.Serialize(sendMessage)}" });
            }
        }
        catch (FlurlHttpException ex)
        {
            var error = await ex.GetResponseJsonAsync<FirstPointResponse>();

            // Let the caller know that delivery status for specific numbers.
            return TypedResults.BadRequest(new SendMessageResponse { Message = $"Failed to submit message to FirstPoint. {System.Text.Json.JsonSerializer.Serialize(error)}" });
        }

    })
        .RequireAuthorization().WithOpenApi(x => new(x) { Summary = "Send an SMS Message.", Description = "Submit outbound messages to this endpoint. The 'to' field is a comma separated list of dialed numbers, or a single dialed number without commas. The 'msisdn' the dialed number of the client that is sending the message. The 'message' field is a string. No validation of the message field occurs before it is forwarded to our upstream vendors." });

    app.MapPost("1pcom/outbound/MMS_Send", async Task<Results<Ok<SendMessageResponse>, BadRequest<SendMessageResponse>>> (FirstPointMMSRequest message, MessagingContext db) =>
    {
        // See emails from Endstream for docs
        // Validate and regularize the incoming message.
        if (!message.RegularizeAndValidate())
        {
            return TypedResults.BadRequest(new SendMessageResponse { Message = "Phone Numbers could not be parsed as valid NANP (North American Numbering Plan) numbers." });
        }

        try
        {
            var sendMessage = await firstPointMMSOutbound
                    .PostUrlEncodedAsync(new
                    {
                        username = firstPointUsername,
                        password = firstPointPassword,
                        message.ani,
                        message.recip,
                        message.ufile
                    })
                    .ReceiveJson<FirstPointResponse>();

            if (sendMessage is not null && sendMessage?.Response?.Text is "OK")
            {
                var record = new MessageRecord
                {
                    Id = Guid.NewGuid(),
                    Content = string.Empty,
                    DateReceivedUTC = DateTime.UtcNow,
                    From = message?.FromPhoneNumber?.DialedNumber ?? string.Empty,
                    To = string.Join(',', message?.ToPhoneNumbers?.Select(x => x.DialedNumber) ?? Array.Empty<string>()),
                    MediaURLs = string.Empty,
                    MessageSource = MessageSource.Outgoing,
                    MessageType = MessageType.MMS,
                    DLRID = sendMessage.Response.DLRID
                };

                record.ToFromCompound = $"{record.From},{record.To}";

                db.Messages.Add(record);
                await db.SaveChangesAsync();

                // Let the caller know that delivery status for specific numbers.
                return TypedResults.Ok(new SendMessageResponse
                {
                    Message = $"Message sent to {record.To}",
                    MessageSent = true,
                });
            }
            else
            {
                return TypedResults.BadRequest(new SendMessageResponse { Message = $"Failed to submit message to FirstPoint. {System.Text.Json.JsonSerializer.Serialize(sendMessage)}" });
            }
        }
        catch (FlurlHttpException ex)
        {
            var error = await ex.GetResponseJsonAsync<FirstPointResponse>();

            // Let the caller know that delivery status for specific numbers.
            return TypedResults.BadRequest(new SendMessageResponse { Message = $"Failed to submit message to FirstPoint. {System.Text.Json.JsonSerializer.Serialize(error)}" });
        }

    }).RequireAuthorization().WithOpenApi(x => new(x) { Summary = "Send an MMS Message.", Description = "Submit outbound messages to this endpoint." });

    app.MapPost("1pcom/inbound/MMS", async Task<Results<Ok<string>, BadRequest<string>, Ok<ForwardedMessage>, UnauthorizedHttpResult>> (HttpContext context, string token, MessagingContext db) =>
    {
        if (token is not "okereeduePeiquah3yaemohGhae0ie")
        {
            Log.Warning($"Token is not valid. Token: {token} is not okereeduePeiquah3yaemohGhae0ie");
            return TypedResults.Unauthorized();
        }

        try
        {
            FirstPointInbound message = new()
            {
                msisdn = context.Request.Form["msisdn"].ToString(),
                to = context.Request.Form["to"].ToString(),
                message = context.Request.Form["message"].ToString(),
                sessionid = context.Request.Form["sessionid"].ToString(),
                serversecret = context.Request.Form["serversecret"].ToString(),
                timezone = context.Request.Form["timezone"].ToString(),
                origtime = context.Request.Form["origtime"].ToString(),
                fullrecipientlist = context.Request.Form["FullRecipientList"].ToString(),
            };

            string incomingRequest = string.Join(',', context.Request.Form.Select(x => $"{x.Key} : {x.Value}"));
            Log.Information(incomingRequest);

            var MMSDescription = System.Text.Json.JsonSerializer.Deserialize<FirstPointMMSMessage>(message.message);

            // Validate and regularize the incoming message.
            if (!message.RegularizeAndValidate())
            {
                return TypedResults.BadRequest($"Phone Numbers could not be parsed as valid NANP (North American Numbering Plan) numbers. {System.Text.Json.JsonSerializer.Serialize(message)}");
            }

            MessageRecord record = new()
            {
                Content = MMSDescription?.files ?? message.message,
                From = message.FromPhoneNumber.DialedNumber,
                To = string.Join(',', message?.ToPhoneNumbers?.Select(x => x.DialedNumber) ?? Array.Empty<string>()),
                MessageSource = MessageSource.Incoming,
                MessageType = MessageType.MMS,
            };

            string MMSMessagePickupRequest = $"{MMSDescription?.url}&authkey={MMSDescription?.authkey}";

            Log.Information(MMSMessagePickupRequest);

            List<string> mediaURLs = new();

            if (!string.IsNullOrWhiteSpace(MMSDescription?.files))
            {
                // Make sure the bucket exists.
                var spacesConfig = new AmazonS3Config
                {
                    ServiceURL = s3ServiceURL,
                };
                using var spacesClient = new AmazonS3Client(digitalOceanSpacesAccessKey, digitalOceanSpacesSecretKey, spacesConfig);
                using var fileUtil = new TransferUtility(spacesClient);

                foreach (string file in MMSDescription.files.Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(file))
                    {
                        string fileDownloadURL = $"{MMSMessagePickupRequest}&file={file}";
                        Log.Information(fileDownloadURL);
                        using var fileStream = await fileDownloadURL.GetStreamAsync();
                        var fileRequest = new TransferUtilityUploadRequest
                        {
                            BucketName = digitalOceanSpacesBucket,
                            InputStream = fileStream,
                            StorageClass = S3StorageClass.Standard,
                            Key = $"{record.Id}{file}",
                            CannedACL = S3CannedACL.Private,
                        };
                        await fileUtil.UploadAsync(fileRequest);
                        mediaURLs.Add($"{spacesConfig.ServiceURL}{fileRequest.BucketName}/{fileRequest.Key}");
                    }
                }
            }

            record.MediaURLs = string.Join(',', mediaURLs);
            record.ToFromCompound = $"{record.From},{record.To}";
            await db.Messages.AddAsync(record);

            try
            {
                await db.SaveChangesAsync();

                // Handle group messages with potentially multiple client registrations.
                if (message is not null && message.ToPhoneNumbers.Any())
                {
                    List<string> sentNumber = new();
                    foreach (var toNumber in message.ToPhoneNumbers)
                    {
                        var existingRegistration = await db.ClientRegistrations.FirstOrDefaultAsync(x => x.AsDialed == toNumber.DialedNumber);

                        if (existingRegistration is not null && existingRegistration.AsDialed == toNumber.DialedNumber)
                        {
                            try
                            {
                                ForwardedMessage toForward = ForwardedMessage.ToForwardedMessage(message, record.Id, record.Content, mediaURLs.ToArray(), existingRegistration.AsDialed, existingRegistration.ClientSecret);

                                // Add some retry logic
                                // Number of retrys
                                // Successfully delieverd
                                var response = await existingRegistration.CallbackUrl.PostJsonAsync(toForward);
                                Log.Information(await response.GetStringAsync());
                                Log.Information(System.Text.Json.JsonSerializer.Serialize(toForward));
                                sentNumber.Add(toNumber.DialedNumber);
                            }
                            catch (FlurlHttpException ex)
                            {
                                Log.Error(await ex.GetResponseStringAsync());
                                Log.Error(System.Text.Json.JsonSerializer.Serialize(existingRegistration));
                                Log.Error($"Failed to forward message to {toNumber.DialedNumber}");
                            }
                        }
                        else
                        {
                            Log.Warning($"{toNumber.DialedNumber} is not registered as a client.");
                        }
                    }

                    if (sentNumber.Any())
                    {
                        Log.Information(System.Text.Json.JsonSerializer.Serialize(record));
                        return TypedResults.Ok("The incoming message was recieved and forwarded to the client.");
                    }
                }

                Log.Warning(System.Text.Json.JsonSerializer.Serialize(record));
                return TypedResults.BadRequest($"{record.To} is not registered as a client.");
            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest($"Failed to save incoming message to the database. {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.StackTrace ?? "No stacktrace found.");
            Log.Information("Failed to read form data by field name.");
            return TypedResults.BadRequest("Failed to read form data by field name.");
        }

    }).WithOpenApi(x => new(x) { Summary = "Send an MMS Message.", Description = "Submit outbound messages to this endpoint." });

    // Add unit tests for this endpoint to verify its behavior.
    //https://sms.callpipe.com/api/inbound/1pcom?token=okereeduePeiquah3yaemohGhae0ie
    //    { "origtime": "2022-04-17 03:48:00", "msisdn": "15555551212", "to": "14445556543", "sessionid": "tLMOYTAmIFiQvBE6X1g", "timezone": "EST", "message": "Your Lyft code is 12345", "api_version": 0.5, "serversecret": "sekrethere"}
    // When this issue is resolved we can simplify the way that we are recieving data in this endpoint: https://github.com/dotnet/aspnetcore/issues/39430 and https://stackoverflow.com/questions/71047077/net-6-minimal-api-and-multipart-form-data/71048827#71048827
    app.MapPost("/api/inbound/1pcom", async Task<Results<Ok<string>, BadRequest<string>, Ok<ForwardedMessage>, UnauthorizedHttpResult>> (HttpContext context, string token, MessagingContext db) =>
    {
        if (token is not "okereeduePeiquah3yaemohGhae0ie")
        {
            Log.Warning($"Token is not valid. Token: {token} is not okereeduePeiquah3yaemohGhae0ie");
            return TypedResults.Unauthorized();
        }

        try
        {
            FirstPointInbound message = new()
            {
                msisdn = context.Request.Form["msisdn"].ToString(),
                to = context.Request.Form["to"].ToString(),
                message = context.Request.Form["message"].ToString(),
                sessionid = context.Request.Form["sessionid"].ToString(),
                serversecret = context.Request.Form["serversecret"].ToString(),
                timezone = context.Request.Form["timezone"].ToString(),
                origtime = context.Request.Form["origtime"].ToString(),
                fullrecipientlist = context.Request.Form["FullRecipientList"].ToString(),
            };

            // Validate and regularize the incoming message.
            if (!message.RegularizeAndValidate())
            {
                return TypedResults.BadRequest($"Phone Numbers could not be parsed as valid NANP (North American Numbering Plan) numbers. {System.Text.Json.JsonSerializer.Serialize(message)}");
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
            await db.Messages.AddAsync(record);

            try
            {
                await db.SaveChangesAsync();

                // Handle group messages with potentially multiple client registrations.
                if (message is not null && message.ToPhoneNumbers.Any())
                {
                    List<string> sentNumber = new();
                    foreach (var toNumber in message.ToPhoneNumbers)
                    {
                        var existingRegistration = await db.ClientRegistrations.FirstOrDefaultAsync(x => x.AsDialed == toNumber.DialedNumber);

                        if (existingRegistration is not null && existingRegistration.AsDialed == toNumber.DialedNumber)
                        {
                            try
                            {
                                ForwardedMessage toForward = ForwardedMessage.ToForwardedMessage(message, record.Id, record.Content, Array.Empty<string>(), existingRegistration.AsDialed, existingRegistration.ClientSecret);

                                // Add some retry logic
                                // Number of retrys
                                // Successfully delieverd
                                var response = await existingRegistration.CallbackUrl.PostJsonAsync(toForward);
                                Log.Information(await response.GetStringAsync());
                                Log.Information(System.Text.Json.JsonSerializer.Serialize(toForward));
                                sentNumber.Add(toNumber.DialedNumber);
                            }
                            catch (FlurlHttpException ex)
                            {
                                Log.Error(await ex.GetResponseStringAsync());
                                Log.Error(System.Text.Json.JsonSerializer.Serialize(existingRegistration));
                                Log.Error($"Failed to forward message to {toNumber.DialedNumber}");
                            }
                        }
                        else
                        {
                            Log.Warning($"{toNumber.DialedNumber} is not registered as a client.");
                        }
                    }

                    if (sentNumber.Any())
                    {
                        Log.Information(System.Text.Json.JsonSerializer.Serialize(record));
                        return TypedResults.Ok("The incoming message was recieved and forwarded to the client.");
                    }
                }

                Log.Warning(System.Text.Json.JsonSerializer.Serialize(record));
                return TypedResults.BadRequest($"{record.To} is not registered as a client.");

            }
            catch (Exception ex)
            {
                return TypedResults.BadRequest($"Failed to save incoming message to the database. {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.StackTrace ?? "No stacktrace found.");
            Log.Information("Failed to read form data by field name.");
            return TypedResults.BadRequest("Failed to read form data by field name.");
        }
    }).WithOpenApi(x => new(x) { Summary = "For use by First Point Communications only.", Description = "Recieves incoming messages from our upstream provider. Forwards valid SMS messages to clients registered through the /client/register endpoint. Forwarded messages are in the form described by the MessageRecord entry in the Schema's section of this page. The is no request body as the data provided by First Point Communications is UrlEncoded like POSTing form data rather than JSON formatted in body of the POST request. The Token is a secret created and maintained by First Point Communications. This endpoint is not for use by anyone other than First Point Communications. It is documented here to help developers understand how incoming messages are fowarded to the client that they have registered with this API. The Messaging.Tests project is a series of functional tests that verify the behavior of this endpoint, because this method of message passing is so chaotic." });



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

// Without this the test suite won't work.
public partial class Program
{ }

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

    public class FirstPointMMSRequest
    {
        // MSISDN or From Dialed Number
        public string ani { get; set; } = string.Empty;
        // To Dialed Number
        public string recip { get; set; } = string.Empty;
        public byte[]? ufile { get; set; } = null;
        // These are for the regularization of phone numbers and not mapped from the JSON payload.
        [JsonIgnore]
        public PhoneNumbersNA.PhoneNumber FromPhoneNumber { get; set; } = new();
        [JsonIgnore]
        public List<PhoneNumbersNA.PhoneNumber> ToPhoneNumbers { get; set; } = new();

        public bool RegularizeAndValidate()
        {
            bool FromParsed = false;
            bool ToParsed = false;

            if (!string.IsNullOrWhiteSpace(ani))
            {
                var checkFrom = PhoneNumbersNA.PhoneNumber.TryParse(ani, out var fromPhoneNumber);
                if (checkFrom && fromPhoneNumber is not null && !string.IsNullOrWhiteSpace(fromPhoneNumber.DialedNumber))
                {
                    FromPhoneNumber = fromPhoneNumber;
                    ani = $"1{fromPhoneNumber.DialedNumber}";
                    FromParsed = true;
                }
            }

            if (recip is not null && recip.Any())
            {
                // This may not be necessary if this list is always created by the BulkVSMessage constructor.
                ToPhoneNumbers ??= new List<PhoneNumbersNA.PhoneNumber>();

                foreach (var number in recip.Split(','))
                {
                    var checkTo = PhoneNumbersNA.PhoneNumber.TryParse(number, out var toPhoneNumber);

                    if (checkTo && toPhoneNumber is not null)
                    {
                        ToPhoneNumbers.Add(toPhoneNumber);
                    }
                }

                // This will drop the numbers that couldn't be parsed.
                recip = ToPhoneNumbers is not null && ToPhoneNumbers.Any() ? ToPhoneNumbers.Count > 1 ? string.Join(",", ToPhoneNumbers.Select(x => $"1{x.DialedNumber!}")) : $"1{ToPhoneNumbers?.FirstOrDefault()?.DialedNumber}" ?? string.Empty : string.Empty;
                ToParsed = true;
            }

            return FromParsed && ToParsed;
        }
    }

    // This model isn't converted into JSON as First Com expects a form-style URLEncoded POST as a request. Only the response is actually JSON.
    public class SendMessageRequest
    {
        public string To { get; set; } = string.Empty;
        public string MSISDN { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string[] MediaURLs { get; set; } = Array.Empty<string>();
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
                    if (fromPhoneNumber.Type is PhoneNumbersNA.NumberType.ShortCode)
                    {
                        FromPhoneNumber = fromPhoneNumber;
                        MSISDN = fromPhoneNumber.DialedNumber;
                        FromParsed = true;
                    }
                    else
                    {
                        FromPhoneNumber = fromPhoneNumber;
                        MSISDN = $"1{fromPhoneNumber.DialedNumber}";
                        FromParsed = true;
                    }
                }
            }

            if (To is not null && To.Any())
            {
                // This may not be necessary if this list is always created by the BulkVSMessage constructor.
                ToPhoneNumbers ??= new List<PhoneNumbersNA.PhoneNumber>();
                string parsedTo = string.Empty;

                foreach (var number in To.Split(','))
                {
                    var checkTo = PhoneNumbersNA.PhoneNumber.TryParse(number, out var toPhoneNumber);

                    if (checkTo && toPhoneNumber is not null)
                    {
                        ToPhoneNumbers.Add(toPhoneNumber);
                        if (toPhoneNumber.Type is PhoneNumbersNA.NumberType.ShortCode)
                        {
                            parsedTo += $"{toPhoneNumber.DialedNumber}";
                        }
                        else
                        {
                            parsedTo += $"1{toPhoneNumber.DialedNumber}";
                        }
                    }
                }

                // This will drop the numbers that couldn't be parsed.
                To = string.IsNullOrWhiteSpace(parsedTo) ? To : parsedTo;
                ToParsed = true;
            }

            return FromParsed && ToParsed;
        }
    }

    public class FirstPointMMSMessage
    {
        public string authkey { get; set; } = string.Empty;
        public string encoding { get; set; } = string.Empty;
        public string files { get; set; } = string.Empty;
        public string recip { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
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
        public double api_version { get; set; }
        public string serversecret { get; set; } = string.Empty;
        // These are for the regularization of phone numbers and not mapped from the JSON payload.
        public string fullrecipientlist { get; set; } = string.Empty;
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
                bool checkFrom = PhoneNumbersNA.PhoneNumber.TryParse(msisdn, out var fromPhoneNumber);
                if (checkFrom && fromPhoneNumber is not null && !string.IsNullOrWhiteSpace(fromPhoneNumber.DialedNumber))
                {
                    if (fromPhoneNumber.Type is not PhoneNumbersNA.NumberType.ShortCode)
                    {
                        FromPhoneNumber = fromPhoneNumber;
                        msisdn = $"1{fromPhoneNumber.DialedNumber}";
                        FromParsed = true;
                    }
                    else
                    {
                        FromPhoneNumber = fromPhoneNumber;
                        msisdn = fromPhoneNumber.DialedNumber;
                        FromParsed = true;
                    }
                }
            }

            if (to is not null && to.Any())
            {
                // This may not be necessary if this list is always created by the BulkVSMessage constructor.
                ToPhoneNumbers ??= new List<PhoneNumbersNA.PhoneNumber>();
                string parsedTo = string.Empty;

                foreach (var number in to.Split(','))
                {
                    var checkTo = PhoneNumbersNA.PhoneNumber.TryParse(number, out var toPhoneNumber);

                    if (checkTo && toPhoneNumber is not null)
                    {
                        ToPhoneNumbers.Add(toPhoneNumber);
                        if (toPhoneNumber.Type is PhoneNumbersNA.NumberType.ShortCode)
                        {
                            parsedTo = string.IsNullOrWhiteSpace(parsedTo) ? $"{toPhoneNumber.DialedNumber}" : $",{toPhoneNumber.DialedNumber}";
                        }
                        else
                        {
                            parsedTo += string.IsNullOrWhiteSpace(parsedTo) ? $"1{toPhoneNumber.DialedNumber}" : $",1{toPhoneNumber.DialedNumber}";
                        }
                    }
                }

                // This will drop the numbers that couldn't be parsed.
                to = string.IsNullOrWhiteSpace(parsedTo) ? to : parsedTo;
                ToParsed = true;
            }

            if (to is not null && fullrecipientlist is not null && fullrecipientlist.Any())
            {
                string parsedTo = string.Empty;

                foreach (var number in fullrecipientlist.Split(','))
                {
                    var checkTo = PhoneNumbersNA.PhoneNumber.TryParse(number, out var toPhoneNumber);

                    if (checkTo && toPhoneNumber is not null)
                    {
                        // Prevent adding duplicates to the To list.
                        bool checkExists = ToPhoneNumbers.Exists(x => x.DialedNumber == toPhoneNumber.DialedNumber);
                        if (!checkExists)
                        {
                            ToPhoneNumbers.Add(toPhoneNumber);
                            if (toPhoneNumber.Type is PhoneNumbersNA.NumberType.ShortCode)
                            {
                                parsedTo = string.IsNullOrWhiteSpace(parsedTo) ? $"{toPhoneNumber.DialedNumber}" : $",{toPhoneNumber.DialedNumber}";
                            }
                            else
                            {
                                parsedTo += string.IsNullOrWhiteSpace(parsedTo) ? $"1{toPhoneNumber.DialedNumber}" : $",1{toPhoneNumber.DialedNumber}";
                            }
                        }
                    }
                }

                // This will drop the numbers that couldn't be parsed.
                to = string.IsNullOrWhiteSpace(parsedTo) ? to : parsedTo;
                ToParsed = true;
            }

            return FromParsed && ToParsed;
        }
    }

    public class SendMessageResponse
    {
        [DefaultValue(false)]
        public bool MessageSent { get; set; } = false;
        [DataType(DataType.Text)]
        public string Message { get; set; } = string.Empty;
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
        [DataType(DataType.DateTime)]
        public DateTime DateReceivedUTC { get; set; } = DateTime.UtcNow;
        [DataType(DataType.Text)]
        public string DLRID { get; set; } = string.Empty;
        [DataType(DataType.Password)]
        public string ClientSecret { get; set; } = string.Empty;
    }

    // Format forward to client apps as JSON.
    public class ForwardedMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string[] AdditionalRecipients { get; set; } = Array.Empty<string>();
        public string Content { get; set; } = string.Empty;
        public string[] MediaURLs { get; set; } = Array.Empty<string>();
        public MessageType MessageType { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime DateReceivedUTC { get; set; } = DateTime.UtcNow;
        [DataType(DataType.Password)]
        public string ClientSecret { get; set; } = string.Empty;

        public static ForwardedMessage ToForwardedMessage(FirstPointInbound parsed, Guid recordId, string textMessage, string[] mediaURLs, string primaryTo, string secret)
        {
            return new()
            {
                Id = recordId,
                From = parsed.FromPhoneNumber.DialedNumber,
                To = primaryTo,
                AdditionalRecipients = parsed.ToPhoneNumbers.Where(x => x.DialedNumber != primaryTo).Select(x => x.DialedNumber).ToArray(),
                Content = textMessage,
                MediaURLs = mediaURLs,
                MessageType = mediaURLs.Any() ? MessageType.MMS : MessageType.SMS,
                DateReceivedUTC = DateTime.UtcNow,
                ClientSecret = secret,
            };
        }

        public MessageRecord ToMessageRecord()
        {
            return new()
            {
                Id = Id,
                From = From,
                To = $"{To},{string.Join(',', AdditionalRecipients)}",
                Content = Content,
                MediaURLs = string.Join(',', MediaURLs),
                MessageType = MessageType,
                DateReceivedUTC = DateReceivedUTC,
            };
        }
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
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        [DataType(DataType.Text)]
        public string Username { get; set; } = string.Empty;
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;
        [DataType(DataType.Text)]
        public string Token { get; set; } = string.Empty;
    }

    public class RegistrationRequest
    {
        [DataType(DataType.PhoneNumber)]
        public string DialedNumber { get; set; } = string.Empty;
        [DataType(DataType.Url)]
        public string CallbackUrl { get; set; } = string.Empty;
        [DataType(DataType.Password)]
        public string ClientSecret { get; set; } = string.Empty;
    }

    public class RegistrationResponse
    {
        [DataType(DataType.PhoneNumber)]
        public string DialedNumber { get; set; } = string.Empty;
        [DataType(DataType.Url)]
        public string CallbackUrl { get; set; } = string.Empty;
        [DefaultValue(false)]
        public bool Registered { get; set; } = false;
        [DataType(DataType.Text)]
        public string Message { get; set; } = string.Empty;
    }

    public class ClientRegistration
    {
        [Key]
        public Guid ClientRegistrationId { get; set; } = Guid.NewGuid();
        [DataType(DataType.PhoneNumber)]
        public string AsDialed { get; set; } = string.Empty;
        [DataType(DataType.Url)]
        public string CallbackUrl { get; set; } = string.Empty;
        [DataType(DataType.DateTime)]
        public DateTime DateRegistered { get; set; } = DateTime.Now;
        [DataType(DataType.Password)]
        public string ClientSecret { get; set; } = string.Empty;
    }
}