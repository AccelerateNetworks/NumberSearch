using Amazon.S3;
using Amazon.S3.Transfer;

using FirstCom;

using Flurl.Http;

using Messaging;

using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Models;

using Npgsql;

using Prometheus;

using Serilog;
using Serilog.Events;

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
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
    var config = builder.Configuration.Get<AppSettings>();
    builder.Services.AddSingleton(config ?? new());
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
    string firstPointIncomingToken = builder.Configuration.GetConnectionString("PComNetIncomingToken") ?? string.Empty;
    string firstPointIncomingMMSSecret = builder.Configuration.GetConnectionString("PComNetIncomingMMSSecret") ?? string.Empty;
    string firstPointIncomingSMSSecret = builder.Configuration.GetConnectionString("PComNetIncomingSMSSecret") ?? string.Empty;

    string localSecret = builder.Configuration.GetConnectionString("MessagingAPISecret") ?? string.Empty;
    string digitalOceanSpacesBucket = builder.Configuration.GetConnectionString("BucketName") ?? string.Empty;
    string digitalOceanSpacesAccessKey = builder.Configuration.GetConnectionString("DOSpacesAccessKey") ?? string.Empty;
    string digitalOceanSpacesSecretKey = builder.Configuration.GetConnectionString("DOSpacesSecretKey") ?? string.Empty;
    string s3ServiceURL = builder.Configuration.GetConnectionString("S3ServiceURL") ?? string.Empty;

    string emailUsername = builder.Configuration.GetConnectionString("EmailUsername") ?? string.Empty;
    string emailPassword = builder.Configuration.GetConnectionString("EmailPassword") ?? string.Empty;
    string carbonCopyEmail = builder.Configuration.GetConnectionString("CarbonCopyEmail") ?? string.Empty;

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
            Description = "This API abstracts the sending and receiving of SMS/MMS messages to and from our upstream vendors.",
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
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddApiEndpoints();

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    builder.Services.AddAuthentication().AddBearerToken(IdentityConstants.BearerScheme);

    builder.Services.AddAuthorizationBuilder().AddPolicy("api", p =>
    {
        p.RequireAuthenticatedUser();
        p.AddAuthenticationSchemes(IdentityConstants.BearerScheme);
    });

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
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
    builder.Services.AddAWSService<IAmazonS3>();

    //builder.Services.AddHttpLogging(httpLogging =>
    // {
    //     httpLogging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    //     httpLogging.RequestBodyLogLimit = 4096;
    //     httpLogging.ResponseBodyLogLimit = 4096;
    // });

    var app = builder.Build();

    // Create the database if it doesn't exist
    var contextOptions = new DbContextOptionsBuilder<MessagingContext>()
        .UseSqlite()
        .Options;
    using var dbContext = new MessagingContext(contextOptions);
    await dbContext.Database.MigrateAsync();

    app.UseCors();
    app.UseAuthentication();
    //app.UseHttpLogging();

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

    app.UseSerilogRequestLogging();
    app.UseSecurityHeaders();
    app.UseHttpMetrics();
    app.MapMetrics();

    app.MapPost("/login", async Task<Results<Ok<AccessTokenResponse>, EmptyHttpResult, ProblemHttpResult>>
        ([FromBody] LoginRequest login, [FromQuery] bool? useCookies, [FromQuery] bool? useSessionCookies, [FromServices] IServiceProvider sp) =>
    {
        var signInManager = sp.GetRequiredService<SignInManager<IdentityUser>>();

        var useCookieScheme = (useCookies == true) || (useSessionCookies == true);
        var isPersistent = (useCookies == true) && (useSessionCookies != true);
        signInManager.AuthenticationScheme = useCookieScheme ? IdentityConstants.ApplicationScheme : IdentityConstants.BearerScheme;

        var result = await signInManager.PasswordSignInAsync(login.Email, login.Password, isPersistent, lockoutOnFailure: true);

        if (result.RequiresTwoFactor)
        {
            if (!string.IsNullOrEmpty(login.TwoFactorCode))
            {
                result = await signInManager.TwoFactorAuthenticatorSignInAsync(login.TwoFactorCode, isPersistent, rememberClient: isPersistent);
            }
            else if (!string.IsNullOrEmpty(login.TwoFactorRecoveryCode))
            {
                result = await signInManager.TwoFactorRecoveryCodeSignInAsync(login.TwoFactorRecoveryCode);
            }
        }

        if (!result.Succeeded)
        {
            return TypedResults.Problem(result.ToString(), statusCode: StatusCodes.Status401Unauthorized);
        }

        // The signInManager already produced the needed response in the form of a cookie or bearer token.
        return TypedResults.Empty;
    });

    app.MapPost("/refresh", async Task<Results<Ok<AccessTokenResponse>, UnauthorizedHttpResult, SignInHttpResult, ChallengeHttpResult>>
        ([FromBody] RefreshRequest refreshRequest, [FromServices] IServiceProvider sp) =>
    {
        var bearerTokenOptions = sp.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>();
        var timeProvider = sp.GetRequiredService<TimeProvider>();

        var signInManager = sp.GetRequiredService<SignInManager<IdentityUser>>();
        var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        var refreshTicket = refreshTokenProtector.Unprotect(refreshRequest.RefreshToken);

        // Reject the /refresh attempt with a 401 if the token expired or the security stamp validation fails
        if (refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc ||
            timeProvider.GetUtcNow() >= expiresUtc ||
            await signInManager.ValidateSecurityStampAsync(refreshTicket.Principal) is not IdentityUser user)

        {
            return TypedResults.Challenge();
        }

        var newPrincipal = await signInManager.CreateUserPrincipalAsync(user);
        return TypedResults.SignIn(newPrincipal, authenticationScheme: IdentityConstants.BearerScheme);
    });

    // These two endpoints /login and /refresh were extracted from  app.MapGroup("/auth").MapIdentityApi<IdentityUser>(); to hide the registration endpoint.

    app.MapGet("/client", MessagingClient.GetByDialedNumberAsync)
        .RequireAuthorization("api")
        .WithOpenApi(x => new(x)
        {
            Summary = "Lookup a specific client registration using the dialed number.",
            Description = "Use this to see if a dialed number is registered and find out what callback Url its registered to."
        });


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
        .RequireAuthorization("api").WithOpenApi(x => new(x) { Summary = "View all registered clients.", Description = "This is intended to be used for debugging client registrations." });

    app.MapGet("/client/usage", async Task<Results<Ok<UsageSummary[]>, BadRequest<string>, NotFound<string>>> (MessagingContext db, string? asDialed) =>
    {
        if (string.IsNullOrWhiteSpace(asDialed))
        {
            try
            {
                var registrations = await db.ClientRegistrations.ToArrayAsync();
                List<UsageSummary> summary = new()
                {
                    new UsageSummary
                    {
                        AsDialed = "Total",
                        InboundMMSCount = await db.Messages.Where(x => x.MessageSource == MessageSource.Incoming && x.MessageType == MessageType.MMS).CountAsync(),
                        OutboundMMSCount = await db.Messages.Where(x => x.MessageSource == MessageSource.Outgoing && x.MessageType == MessageType.MMS).CountAsync(),
                        InboundSMSCount = await db.Messages.Where(x => x.MessageSource == MessageSource.Incoming && x.MessageType == MessageType.SMS).CountAsync(),
                        OutboundSMSCount = await db.Messages.Where(x => x.MessageSource == MessageSource.Outgoing && x.MessageType == MessageType.SMS).CountAsync()
                    }
                };

                foreach (var reg in registrations)
                {
                    var x = reg.UpstreamStatusDescription;
                    var y = reg.RegisteredUpstream;
                    var checkAsDialed = PhoneNumbersNA.PhoneNumber.TryParse(reg.AsDialed, out var asDialedNumber);

                    if (asDialedNumber.Type is not PhoneNumbersNA.NumberType.ShortCode)
                    {
                        asDialed = $"1{asDialedNumber.DialedNumber}";
                    }
                    else
                    {
                        asDialed = asDialedNumber.DialedNumber;
                    }

                    int inboundMMS = await db.Messages.Where(x => (x.From == asDialed || x.To.Contains(reg.AsDialed)) && x.MessageSource == MessageSource.Incoming && x.MessageType == MessageType.MMS).CountAsync();
                    int outboundMMS = await db.Messages.Where(x => (x.From == asDialed || x.To.Contains(reg.AsDialed)) && x.MessageSource == MessageSource.Outgoing && x.MessageType == MessageType.MMS).CountAsync();
                    int inboundSMS = await db.Messages.Where(x => (x.From == asDialed || x.To.Contains(reg.AsDialed)) && x.MessageSource == MessageSource.Incoming && x.MessageType == MessageType.SMS).CountAsync();
                    int outboundSMS = await db.Messages.Where(x => (x.From == asDialed || x.To.Contains(reg.AsDialed)) && x.MessageSource == MessageSource.Outgoing && x.MessageType == MessageType.SMS).CountAsync();
                    summary.Add(new UsageSummary
                    {
                        AsDialed = reg.AsDialed,
                        InboundMMSCount = inboundMMS,
                        OutboundMMSCount = outboundMMS,
                        InboundSMSCount = inboundSMS,
                        OutboundSMSCount = outboundSMS,
                        RegisteredUpstream = reg.RegisteredUpstream,
                        UpstreamStatusDescription = reg.UpstreamStatusDescription,
                    });
                }

                if (summary.Count > 0)
                {
                    return TypedResults.Ok(summary.ToArray());
                }
                else
                {
                    return TypedResults.NotFound($"No clients are currently registered for service.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace ?? "No stack trace found.");
                return TypedResults.BadRequest(ex.Message);
            }
        }
        else
        {
            var checkAsDialed = PhoneNumbersNA.PhoneNumber.TryParse(asDialed, out var asDialedNumber);
            if (!checkAsDialed)
            {
                return TypedResults.BadRequest("AsDialed number couldn't be parsed as a valid NANP (North American Number Plan) number.");
            }

            try
            {
                var reg = await db.ClientRegistrations.FirstOrDefaultAsync(x => x.AsDialed == asDialedNumber.DialedNumber);

                if (reg is not null && !string.IsNullOrWhiteSpace(reg.AsDialed))
                {
                    if (asDialedNumber.Type is not PhoneNumbersNA.NumberType.ShortCode)
                    {
                        asDialed = $"1{asDialedNumber.DialedNumber}";
                    }
                    else
                    {
                        asDialed = asDialedNumber.DialedNumber;
                    }

                    var inboundMMS = await db.Messages.Where(x => (x.To.Contains(asDialed) || x.From == asDialed) && x.MessageSource == MessageSource.Incoming && x.MessageType == MessageType.MMS).CountAsync();
                    var outboundMMS = await db.Messages.Where(x => (x.To.Contains(asDialed) || x.From == asDialed) && x.MessageSource == MessageSource.Outgoing && x.MessageType == MessageType.MMS).CountAsync();
                    var inboundSMS = await db.Messages.Where(x => (x.To.Contains(asDialed) || x.From == asDialed) && x.MessageSource == MessageSource.Incoming && x.MessageType == MessageType.SMS).CountAsync();
                    var outboundSMS = await db.Messages.Where(x => (x.To.Contains(asDialed) || x.From == asDialed) && x.MessageSource == MessageSource.Outgoing && x.MessageType == MessageType.SMS).CountAsync();
                    var summary = new UsageSummary
                    {
                        AsDialed = reg.AsDialed,
                        InboundMMSCount = inboundMMS,
                        OutboundMMSCount = outboundMMS,
                        InboundSMSCount = inboundSMS,
                        OutboundSMSCount = outboundSMS,
                        RegisteredUpstream = reg.RegisteredUpstream,
                        UpstreamStatusDescription = reg.UpstreamStatusDescription,
                    };
                    return TypedResults.Ok(new UsageSummary[] { summary });
                }
                else
                {
                    return TypedResults.NotFound($"Failed to find a client registration for {asDialed}.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace ?? "No stack trace found.");
                return TypedResults.BadRequest(ex.Message);
            }
        }
    })
    .RequireAuthorization("api").WithOpenApi(x => new(x) { Summary = "View all registered clients.", Description = "This is intended to be used for debugging client registrations." });

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

        //if (string.IsNullOrWhiteSpace(registration.CallbackUrl) && !string.IsNullOrWhiteSpace(registration.Email) && !System.Net.Mail.MailAddress.TryCreate(registration.Email, out var address))
        //{
        //    return TypedResults.BadRequest(new RegistrationResponse
        //    {
        //        DialedNumber = registration.DialedNumber,
        //        CallbackUrl = registration.CallbackUrl,
        //        Email = registration.Email,
        //        Registered = false,
        //        Message = $"The Email provided {registration.Email} is invalid or not a well formatted address. Email was parsed as {address?.Address} which is invalid."
        //    });
        //}

        // Validate the callback Url to prevent dumb errors.
        if (!Uri.IsWellFormedUriString(registration.CallbackUrl, UriKind.Absolute) /*&& string.IsNullOrWhiteSpace(registration.Email)*/)
        {
            return TypedResults.BadRequest(new RegistrationResponse
            {
                DialedNumber = registration.DialedNumber,
                CallbackUrl = registration.CallbackUrl,
                Registered = false,
                Message = $"The callback Url provided {registration.CallbackUrl} is invalid or not a well formatted Uri. Please read https://learn.microsoft.com/en-us/dotnet/api/system.uri.iswellformeduristring?view=net-7.0 for more information."
            });
        }

        string message = string.Empty;
        bool registeredUpstream = false;
        string upstreamStatusDescription = string.Empty;
        string dialedNumber = asDialedNumber.Type is not PhoneNumbersNA.NumberType.ShortCode ? $"1{asDialedNumber.DialedNumber}" : asDialedNumber.DialedNumber;

        try
        {
            // Verify that this number is routed through our upstream provider.
            var checkRouted = await FirstPointComSMS.GetSMSRoutingByDialedNumberAsync(dialedNumber, firstPointUsername, firstPointPassword);
            Log.Information(System.Text.Json.JsonSerializer.Serialize(checkRouted));
            registeredUpstream = checkRouted.QueryResult.code is 0 && checkRouted.epid is 265;
            upstreamStatusDescription = checkRouted.QueryResult.text;
            if (checkRouted.QueryResult.code is not 0 || checkRouted.epid is not 265)
            {
                // Enabled routing and set the EPID if the number is not already routed.
                var enableSMS = await FirstPointComSMS.EnableSMSByDialedNumberAsync(dialedNumber, firstPointUsername, firstPointPassword);
                Log.Information(System.Text.Json.JsonSerializer.Serialize(enableSMS));
                var setRouting = await FirstPointComSMS.RouteSMSToEPIDByDialedNumberAsync(dialedNumber, 265, firstPointUsername, firstPointPassword);
                Log.Information(System.Text.Json.JsonSerializer.Serialize(setRouting));
                var checkRoutedAgain = await FirstPointComSMS.GetSMSRoutingByDialedNumberAsync(dialedNumber, firstPointUsername, firstPointPassword);
                Log.Information(System.Text.Json.JsonSerializer.Serialize(checkRouted));
                registeredUpstream = checkRouted.QueryResult.code is 0 && checkRouted.epid is 265;
                upstreamStatusDescription = checkRouted.QueryResult.text;
                message = $"Attempted to set and enable SMS routing for {dialedNumber}. SMS Enabled? {enableSMS.text} Routing Set? {setRouting.text} SMS Routed? {checkRoutedAgain.QueryResult.text}";
            }
            else
            {
                message = $"This number is routed for SMS service with our upstream vendor: {checkRouted.QueryResult.text}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.StackTrace ?? "No stack trace found.");
            return TypedResults.BadRequest(new RegistrationResponse
            {
                DialedNumber = registration.DialedNumber,
                CallbackUrl = registration.CallbackUrl,
                Registered = false,
                Message = $"Failed to enabled messaging service through EndStream for this dialed number. Please email dan@acceleratenetworks.com to report this outage. {ex.Message}"
            });
        }

        try
        {
            // Update existing registrations before creating new ones.
            var existingRegistration = await db.ClientRegistrations.Where(x => x.AsDialed == asDialedNumber.DialedNumber).FirstOrDefaultAsync();

            if (existingRegistration is not null)
            {
                bool updated = false;
                // If they really are different update the record.
                if (existingRegistration.ClientSecret != registration.ClientSecret)
                {
                    existingRegistration.ClientSecret = registration.ClientSecret;
                    updated = true;
                }

                if (existingRegistration.CallbackUrl != registration.CallbackUrl && !string.IsNullOrWhiteSpace(registration.CallbackUrl))
                {
                    existingRegistration.CallbackUrl = registration.CallbackUrl;
                    updated = true;
                }

                if (existingRegistration.RegisteredUpstream != registeredUpstream)
                {
                    existingRegistration.RegisteredUpstream = registeredUpstream;
                    updated = true;
                }

                if (existingRegistration.UpstreamStatusDescription != upstreamStatusDescription)
                {
                    existingRegistration.UpstreamStatusDescription = upstreamStatusDescription;
                    updated = true;
                }

                //if (existingRegistration.Email != registration.Email && !string.IsNullOrWhiteSpace(registration.Email))
                //{
                //    existingRegistration.Email = registration.Email;
                //    updated = true;
                //}

                if (updated)
                {
                    await db.SaveChangesAsync();
                }
                // Otherwise do nothing.
            }
            else
            {
                db.Add(new ClientRegistration
                {
                    AsDialed = asDialedNumber.DialedNumber,
                    CallbackUrl = registration.CallbackUrl,
                    ClientSecret = registration.ClientSecret,
                    RegisteredUpstream = registeredUpstream,
                    UpstreamStatusDescription = upstreamStatusDescription,
                    //Email = registration.Email,
                });
                await db.SaveChangesAsync();
            }

            existingRegistration = await db.ClientRegistrations.FirstOrDefaultAsync(x => x.AsDialed == asDialedNumber.DialedNumber);

            //if (existingRegistration is not null && string.IsNullOrWhiteSpace(existingRegistration.Email) && existingRegistration.EmailVerified is false)
            //{
            //    try
            //    {
            //        var outboundMessage = new MimeMessage
            //        {
            //            Sender = new MailboxAddress("TextToEmail", emailUsername),
            //            Subject = $"Email Verification"
            //        };

            //        // Set the client secret if it was not provided.
            //        if (string.IsNullOrWhiteSpace(registration.ClientSecret))
            //        {
            //            Random r = new Random();
            //            int randNum = r.Next(999999);
            //            registration.ClientSecret = randNum.ToString("D6");
            //        }

            //        var builder = new BodyBuilder
            //        {
            //            HtmlBody = @$"<!DOCTYPE html><html><head><title></title></head><body>Please respond with <b>{registration.ClientSecret}<b> to verify your email address and enroll {registration.DialedNumber} in text to email service from Accelerate Networks.</body></html>"
            //        };

            //        var ordersInbox = MailboxAddress.Parse(emailUsername);
            //        var recipient = MailboxAddress.Parse(existingRegistration.Email);

            //        outboundMessage.From.Add(ordersInbox);
            //        outboundMessage.To.Add(recipient);

            //        if (!string.IsNullOrWhiteSpace(carbonCopyEmail) && carbonCopyEmail.Contains("@"))
            //        {
            //            var cc = MailboxAddress.Parse(carbonCopyEmail);
            //            outboundMessage.Cc.Add(cc);
            //        }

            //        outboundMessage.Body = builder.ToMessageBody();

            //        using var smtp = new MailKit.Net.Smtp.SmtpClient();
            //        smtp.MessageSent += (sender, args) => { };
            //        smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            //        await smtp.ConnectAsync("witcher.mxrouting.net", 587, SecureSocketOptions.StartTls).ConfigureAwait(false);
            //        await smtp.AuthenticateAsync(emailUsername, emailPassword).ConfigureAwait(false);
            //        await smtp.SendAsync(outboundMessage).ConfigureAwait(false);
            //        await smtp.DisconnectAsync(true).ConfigureAwait(false);
            //    }
            //    catch (Exception ex)
            //    {
            //        Log.Fatal($"[Email] Failed to send email.");
            //        Log.Fatal(ex.Message);
            //        Log.Fatal(ex.StackTrace ?? "StackTrace was null.");
            //    }
            //}

            // Forward failed incoming messages for this number.
            var inboundMMS = await db.Messages.Where(x => x.To.Contains(asDialedNumber.DialedNumber) && x.MessageSource == MessageSource.Incoming && x.MessageType == MessageType.MMS && !x.Succeeded).ToListAsync();
            var inboundSMS = await db.Messages.Where(x => x.To.Contains(asDialedNumber.DialedNumber) && x.MessageSource == MessageSource.Incoming && x.MessageType == MessageType.SMS && !x.Succeeded).ToListAsync();
            inboundMMS.AddRange(inboundSMS);

            // Cap the replaying of messages to 10 messages or messages received in the last 2 weeks.
            inboundMMS = inboundMMS.Where(x => x.DateReceivedUTC > DateTime.UtcNow.AddDays(-14)).Take(30).ToList();
            foreach (var failedMessage in inboundMMS)
            {
                try
                {
                    // Forward to the newly registered callback URL.
                    var messageToForward = System.Text.Json.JsonSerializer.Deserialize<ForwardedMessage>(failedMessage.ToForward);

                    if (messageToForward is not null && !string.IsNullOrWhiteSpace(messageToForward.Content) && !string.IsNullOrWhiteSpace(registration.CallbackUrl))
                    {
                        messageToForward.ClientSecret = registration.ClientSecret;
                        var response = await registration.CallbackUrl.PostJsonAsync(messageToForward);
                        string responseText = await response.GetStringAsync();
                        Log.Information(responseText);
                        Log.Information(System.Text.Json.JsonSerializer.Serialize(messageToForward));
                        failedMessage.RawResponse = responseText;
                        failedMessage.Succeeded = true;
                        await db.SaveChangesAsync();
                    }
                    // Do not forward failed messages to email addresses.
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    Log.Error(ex.StackTrace ?? "No stack trace found.");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.StackTrace ?? "No stack trace found.");
            return TypedResults.BadRequest(new RegistrationResponse
            {
                DialedNumber = registration.DialedNumber,
                CallbackUrl = registration.CallbackUrl,
                Registered = false,
                Message = $"Failed to save the registration to the database. Please email dan@acceleratenetworks.com to report this outage. {ex.Message}"
            });
        }

        return TypedResults.Ok(new RegistrationResponse { DialedNumber = dialedNumber, CallbackUrl = registration.CallbackUrl, /*Email = registration.Email,*/ Registered = true, Message = message, RegisteredUpstream = registeredUpstream, UpstreamStatusDescription = upstreamStatusDescription });

    })
        .RequireAuthorization("api").WithOpenApi(x => new(x) { Summary = "Register a client for message forwarding.", Description = "Boy I wish I had more to say about this, lmao." });

    app.MapPost("/client/remove", async Task<Results<Ok<string>, BadRequest<string>>> (string asDialed, MessagingContext db) =>
    {
        var checkAsDialed = PhoneNumbersNA.PhoneNumber.TryParse(asDialed, out var asDialedNumber);
        if (!checkAsDialed)
        {
            return TypedResults.BadRequest($"Dialed number {asDialed} couldn't be parsed as a valid NANP (North American Number Plan) number.");
        }

        string message = string.Empty;
        string dialedNumber = asDialedNumber.Type is not PhoneNumbersNA.NumberType.ShortCode ? $"1{asDialedNumber.DialedNumber}" : asDialedNumber.DialedNumber;

        try
        {
            // Update existing registrations before creating new ones.
            var existingRegistration = await db.ClientRegistrations.Where(x => x.AsDialed == asDialedNumber.DialedNumber).FirstOrDefaultAsync();

            if (existingRegistration is not null)
            {
                db.Remove(existingRegistration);
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.StackTrace ?? "No stack trace found.");
            return TypedResults.BadRequest($"Failed to save the registration to the database. Please email dan@acceleratenetworks.com to report this outage. {ex.Message}");
        }

        return TypedResults.Ok($"The registration for {asDialed} has been removed.");
    })
    .RequireAuthorization("api").WithOpenApi(x => new(x) { Summary = "Register a client for message forwarding.", Description = "Boy I wish I had more to say about this, lmao." });


    app.MapGet("/message/all", async Task<Results<Ok<MessageRecord[]>, NotFound<string>, BadRequest<string>>> (MessagingContext db, string? asDialed) =>
    {
        if (string.IsNullOrWhiteSpace(asDialed))
        {
            try
            {
                var messages = await db.Messages.OrderByDescending(x => x.DateReceivedUTC).Take(100).ToArrayAsync();

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
        }
        else
        {
            try
            {
                bool checkFrom = PhoneNumbersNA.PhoneNumber.TryParse(asDialed, out var fromPhoneNumber);
                if (checkFrom && fromPhoneNumber is not null && !string.IsNullOrWhiteSpace(fromPhoneNumber.DialedNumber))
                {
                    if (fromPhoneNumber.Type is not PhoneNumbersNA.NumberType.ShortCode)
                    {
                        asDialed = $"1{fromPhoneNumber.DialedNumber}";
                    }
                    else
                    {
                        asDialed = fromPhoneNumber.DialedNumber;
                    }

                    var messages = await db.Messages.Where(x => x.From == asDialed || x.To.Contains(asDialed)).OrderByDescending(x => x.DateReceivedUTC).ToArrayAsync();

                    if (messages is not null && messages.Any())
                    {
                        return TypedResults.Ok(messages);
                    }
                    else
                    {
                        return TypedResults.NotFound($"No messages have been recorded.");
                    }
                }
                else
                {
                    return TypedResults.BadRequest($"asDialed {asDialed} could not be parsed as valid NANP (North American Numbering Plan) number.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace ?? "No stacktrace found.");
                return TypedResults.BadRequest(ex.Message);
            }
        }
    })
        .RequireAuthorization("api").WithOpenApi(x => new(x) { Summary = "View all sent and received messages.", Description = "This is intended to help you debug problems with message sending and delivery so you can see if it's this API or the upstream vendor that is causing problems." });

    app.MapGet("/message/all/failed", async Task<Results<Ok<MessageRecord[]>, NotFound<string>, BadRequest<string>>> (MessagingContext db, DateTime start, DateTime end) =>
    {
        try
        {
            var messages = await db.Messages.Where(x => !x.Succeeded && x.DateReceivedUTC > start && x.DateReceivedUTC <= end).OrderByDescending(x => x.DateReceivedUTC).ToArrayAsync();

            if (messages is not null && messages.Any())
            {
                return TypedResults.Ok(messages);
            }
            else
            {
                return TypedResults.NotFound($"No failed messages have been recorded between {start} and {end}.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.StackTrace ?? "No stack trace found.");
            return TypedResults.BadRequest(ex.Message);
        }
    })
    .RequireAuthorization("api").WithOpenApi(x => new(x) { Summary = "View all sent and received messages that failed.", Description = "This is intended to help you debug problems with message sending and delivery so you can see if it's this API or the upstream vendor that is causing problems." });

    app.MapPost("/message/replay", async Task<Results<Ok<string>, NotFound<string>, BadRequest<string>>> (Guid id, MessagingContext db) =>
    {
        try
        {
            var messageRecord = await db.Messages.FirstOrDefaultAsync(x => x.Id == id && x.MessageSource == MessageSource.Incoming);
            if (messageRecord is not null && messageRecord.Id == id && !string.IsNullOrWhiteSpace(messageRecord.ToForward))
            {
                var toForward = JsonSerializer.Deserialize<ForwardedMessage>(messageRecord.ToForward);
                var checkTo = PhoneNumbersNA.PhoneNumber.TryParse(toForward.To, out var toRegisteredNumber);

                if (toForward is not null && checkTo)
                {
                    var client = await db.ClientRegistrations.FirstOrDefaultAsync(x => x.AsDialed == toRegisteredNumber.DialedNumber);

                    if (client is not null && client.AsDialed == toRegisteredNumber.DialedNumber)
                    {
                        toForward.ClientSecret = client.ClientSecret;
                        messageRecord.To = toForward.To;
                        messageRecord.From = toForward.From;
                        messageRecord.ToForward = System.Text.Json.JsonSerializer.Serialize(toForward);

                        try
                        {
                            var response = await client.CallbackUrl.PostJsonAsync(toForward);
                            string responseText = await response.GetStringAsync();
                            Log.Information(responseText);
                            Log.Information(System.Text.Json.JsonSerializer.Serialize(toForward));
                            messageRecord.RawResponse = responseText;
                            messageRecord.Succeeded = true;
                        }
                        catch (FlurlHttpException ex)
                        {
                            string error = await ex.GetResponseStringAsync();
                            Log.Error(error);
                            Log.Error(System.Text.Json.JsonSerializer.Serialize(client));
                            Log.Error(System.Text.Json.JsonSerializer.Serialize(toForward));
                            Log.Error($"Failed to forward message to {toForward.To}");
                            messageRecord.RawResponse = $"Failed to forward message to {toForward.To} at {client.CallbackUrl} {ex.StatusCode} {error}";
                        }

                        try
                        {
                            await db.Messages.AddAsync(messageRecord);
                            await db.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            return TypedResults.BadRequest($"Failed to save incoming message to the database. {ex.Message} {System.Text.Json.JsonSerializer.Serialize(messageRecord)}");
                        }

                        Log.Information(System.Text.Json.JsonSerializer.Serialize(messageRecord));
                        return TypedResults.Ok("The incoming message was replayed and forwarded to the client.");
                    }
                    else
                    {
                        Log.Warning($"{toForward.To} is not registered as a client.");

                        messageRecord.To = toForward.To;
                        messageRecord.From = toForward.From;
                        messageRecord.Content = toForward.Content;
                        messageRecord.ToForward = System.Text.Json.JsonSerializer.Serialize(toForward);
                        messageRecord.RawResponse = $"{toForward.To} is not registered as a client.";
                        db.Messages.Add(messageRecord);
                        await db.SaveChangesAsync();

                        return TypedResults.BadRequest($"{toForward.To} is not registered as a client.");
                    }
                }
                else
                {
                    return TypedResults.BadRequest($"{messageRecord.ToForward} couldn't be parsed into an object from JSON. Please file an issue on GitHub.");
                }
            }
            else
            {
                return TypedResults.NotFound($"No inbound message with an Id of {id} was found.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.StackTrace ?? "No stack trace found.");
            return TypedResults.BadRequest(ex.Message);
        }
    })
    .RequireAuthorization("api").WithOpenApi(x => new(x) { Summary = "Replay an inbound message.", Description = "This is intended to help you debug problems with message delivery so you can see if it's this API or the client app that is causing problems." });

    app.MapPost("/message/send", async Task<Results<Ok<SendMessageResponse>, BadRequest<SendMessageResponse>>> ([Microsoft.AspNetCore.Mvc.FromBody] SendMessageRequest message, bool? test, MessagingContext db) =>
    {
        var toForward = new toForwardOutbound
        {
            username = firstPointUsername,
            password = firstPointPassword,
            messagebody = message.Message
        };

        var record = new MessageRecord
        {
            Id = Guid.NewGuid(),
            Content = message?.Message ?? string.Empty,
            DateReceivedUTC = DateTime.UtcNow,
            From = message?.MSISDN ?? "MSISDN was blank",
            To = message?.To ?? "To was blank",
            MediaURLs = string.Empty,
            MessageSource = MessageSource.Outgoing,
            MessageType = message?.MediaURLs.Length > 0 ? MessageType.MMS : MessageType.SMS,
            RawRequest = System.Text.Json.JsonSerializer.Serialize(message),
            RawResponse = $"MSISDN {message?.MSISDN} could not be parsed as valid NANP (North American Numbering Plan) number.",
        };

        if (!string.IsNullOrWhiteSpace(message.MSISDN))
        {
            bool checkFrom = PhoneNumbersNA.PhoneNumber.TryParse(message.MSISDN, out var fromPhoneNumber);
            if (checkFrom && fromPhoneNumber is not null && !string.IsNullOrWhiteSpace(fromPhoneNumber.DialedNumber))
            {
                if (fromPhoneNumber.Type is not PhoneNumbersNA.NumberType.ShortCode)
                {
                    toForward.msisdn = $"1{fromPhoneNumber.DialedNumber}";
                }
                else
                {
                    toForward.msisdn = fromPhoneNumber.DialedNumber;
                }
            }
            else
            {
                try
                {
                    record = new MessageRecord
                    {
                        Id = Guid.NewGuid(),
                        Content = message?.Message ?? string.Empty,
                        DateReceivedUTC = DateTime.UtcNow,
                        From = message?.MSISDN ?? "MSISDN was blank",
                        To = message?.To ?? "To was blank",
                        MediaURLs = string.Empty,
                        MessageSource = MessageSource.Outgoing,
                        MessageType = message?.MediaURLs.Length > 0 ? MessageType.MMS : MessageType.SMS,
                        RawRequest = System.Text.Json.JsonSerializer.Serialize(message),
                        RawResponse = $"MSISDN {message?.MSISDN} could not be parsed as valid NANP (North American Numbering Plan) number.",
                    };

                    db.Messages.Add(record);
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // If this throws an exception then there is really nothing we can do as the DB is down and there's nothing to save to.
                    Log.Fatal(ex.Message);
                }

                return TypedResults.BadRequest(new SendMessageResponse { Message = $"MSISDN {message?.MSISDN} could not be parsed as valid NANP (North American Numbering Plan) number. {System.Text.Json.JsonSerializer.Serialize(message)}" });
            }
        }

        if (!string.IsNullOrWhiteSpace(message.To))
        {
            List<string> numbers = new();

            foreach (var number in message.To.Split(','))
            {
                var checkTo = PhoneNumbersNA.PhoneNumber.TryParse(number, out var toPhoneNumber);

                if (checkTo && toPhoneNumber is not null)
                {
                    var formattedNumber = toPhoneNumber.Type is PhoneNumbersNA.NumberType.ShortCode ? $"{toPhoneNumber.DialedNumber}" : $"1{toPhoneNumber.DialedNumber}";
                    // Prevent the MSISDN from being included in the the recipients list.
                    if (formattedNumber != toForward.msisdn)
                    {
                        numbers.Add(formattedNumber);
                    }
                }
            }

            if (numbers.Any())
            {
                toForward.to = string.Join(',', numbers);
            }
            else
            {
                try
                {
                    record = new MessageRecord
                    {
                        Id = Guid.NewGuid(),
                        Content = message?.Message ?? string.Empty,
                        DateReceivedUTC = DateTime.UtcNow,
                        From = message?.MSISDN ?? "MSISDN was blank",
                        To = message?.To ?? "To was blank",
                        MediaURLs = string.Empty,
                        MessageSource = MessageSource.Outgoing,
                        MessageType = message?.MediaURLs.Length > 0 ? MessageType.MMS : MessageType.SMS,
                        RawRequest = System.Text.Json.JsonSerializer.Serialize(message),
                        RawResponse = $"To {message?.To} could not be parsed as valid NANP (North American Numbering Plan) number."
                    };

                    db.Messages.Add(record);
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // If this throws an exception then there is really nothing we can do as the DB is down and there's nothing to save to.
                    Log.Fatal(ex.Message);
                }

                return TypedResults.BadRequest(new SendMessageResponse { Message = $"To {message?.To} could not be parsed as valid NANP (North American Numbering Plan) number. {System.Text.Json.JsonSerializer.Serialize(message)}" });
            }
        }

        try
        {
            FirstPointResponse sendMessage = new();
            //Handle MMSes
            if (message.MediaURLs.Length > 0 && !string.IsNullOrWhiteSpace(message.MediaURLs.FirstOrDefault()))
            {
                var multipartContent = new MultipartFormDataContent {
                        { new StringContent(firstPointUsername), "username" },
                        { new StringContent(firstPointPassword), "password" },
                        { new StringContent(toForward.to), "recip" },
                        { new StringContent(toForward.msisdn), "ani" },
                };

                // capture the media urls as byte[]'s
                foreach (string fileURL in message.MediaURLs)
                {
                    var url = new Uri(fileURL);
                    string filename = url.Segments.LastOrDefault() ?? string.Empty;
                    var data = await fileURL.GetBytesAsync();
                    multipartContent.Add(new ByteArrayContent(data), "ufiles", filename);

                    if (filename.Contains(".txt"))
                    {
                        record.Content = await url.GetStringAsync();
                    }
                }

                // pass them on to the vendor
                var MMSResponse = await firstPointMMSOutbound.PostAsync(multipartContent).ReceiveString();

                // Parse the oddly formatted response.
                var toJSON = JsonSerializer.Deserialize<FirstPointResponseMMS>(MMSResponse);
                sendMessage = new FirstPointResponse { Response = toJSON?.Response ?? new() };

                if (sendMessage is not null && sendMessage?.Response?.Text is "OK")
                {
                    record = new MessageRecord
                    {
                        Id = Guid.NewGuid(),
                        DateReceivedUTC = DateTime.UtcNow,
                        From = message?.MSISDN ?? "MSISDN was blank",
                        To = message?.To ?? "To was blank",
                        MediaURLs = string.Empty,
                        MessageSource = MessageSource.Outgoing,
                        MessageType = MessageType.MMS,
                        RawRequest = System.Text.Json.JsonSerializer.Serialize(multipartContent),
                        RawResponse = MMSResponse,
                        Succeeded = true
                    };

                    db.Messages.Add(record);
                    await db.SaveChangesAsync();

                    // Let the caller know that delivery status for specific numbers.
                    return TypedResults.Ok(new SendMessageResponse
                    {
                        Message = $"MMS Message sent to {toForward.to}",
                        MessageSent = true,
                    });
                }
                else
                {
                    record = new MessageRecord
                    {
                        Id = Guid.NewGuid(),
                        DateReceivedUTC = DateTime.UtcNow,
                        From = message?.MSISDN ?? "MSISDN was blank",
                        To = message?.To ?? "To was blank",
                        MediaURLs = string.Empty,
                        MessageSource = MessageSource.Outgoing,
                        MessageType = MessageType.MMS,
                        RawRequest = System.Text.Json.JsonSerializer.Serialize(multipartContent),
                        RawResponse = MMSResponse
                    };

                    db.Messages.Add(record);
                    await db.SaveChangesAsync();

                    return TypedResults.BadRequest(new SendMessageResponse { Message = $"Failed to submit the MMS message to FirstPoint. {MMSResponse}" });
                }
            }
            else
            {
                if (toForward.messagebody.Length > 160)
                {
                    Log.Error($"SMS Message body length exceeded. Length: {toForward.messagebody.Length}");
                    return TypedResults.BadRequest(new SendMessageResponse { Message = $"Failed to submit message to FirstPoint. SMS Message body length exceeded. Length: {toForward.messagebody.Length}", MessageSent = false });
                }
                else
                {
                    sendMessage = test ?? false ? await "https://sms.callpipe.com/message/send/test"
                    .PostUrlEncodedAsync(toForward)
                    .ReceiveJson<FirstPointResponse>() : await firstPointSMSOutbound
                    .PostUrlEncodedAsync(toForward)
                    .ReceiveJson<FirstPointResponse>();
                }

                if (sendMessage is not null && sendMessage?.Response?.Text is "OK")
                {
                    record = new MessageRecord
                    {
                        Id = Guid.NewGuid(),
                        Content = message?.Message ?? string.Empty,
                        DateReceivedUTC = DateTime.UtcNow,
                        From = message?.MSISDN ?? "MSISDN was blank",
                        To = message?.To ?? "To was blank",
                        MediaURLs = string.Empty,
                        MessageSource = MessageSource.Outgoing,
                        MessageType = MessageType.SMS,
                        RawRequest = System.Text.Json.JsonSerializer.Serialize(toForward),
                        RawResponse = System.Text.Json.JsonSerializer.Serialize(sendMessage),
                        Succeeded = true
                    };

                    db.Messages.Add(record);
                    await db.SaveChangesAsync();

                    // Let the caller know that delivery status for specific numbers.
                    return TypedResults.Ok(new SendMessageResponse
                    {
                        Message = $"SMS Message sent to {toForward.to}",
                        MessageSent = true,
                    });
                }
                else
                {
                    record = new MessageRecord
                    {
                        Id = Guid.NewGuid(),
                        Content = message?.Message ?? string.Empty,
                        DateReceivedUTC = DateTime.UtcNow,
                        From = message?.MSISDN ?? "MSISDN was blank",
                        To = message?.To ?? "To was blank",
                        MediaURLs = string.Empty,
                        MessageSource = MessageSource.Outgoing,
                        MessageType = MessageType.SMS,
                        RawRequest = System.Text.Json.JsonSerializer.Serialize(toForward),
                        RawResponse = System.Text.Json.JsonSerializer.Serialize(sendMessage)
                    };

                    db.Messages.Add(record);
                    await db.SaveChangesAsync();

                    return TypedResults.BadRequest(new SendMessageResponse { Message = $"Failed to submit SMS message to FirstPoint. {System.Text.Json.JsonSerializer.Serialize(sendMessage)}" });
                }
            }
        }
        catch (FlurlHttpException ex)
        {
            var error = await ex.GetResponseStringAsync();
            Log.Error(error);

            record = new MessageRecord
            {
                Id = Guid.NewGuid(),
                Content = message?.Message ?? string.Empty,
                DateReceivedUTC = DateTime.UtcNow,
                From = message?.MSISDN ?? "MSISDN was blank",
                To = message?.To ?? "To was blank",
                MediaURLs = string.Empty,
                MessageSource = MessageSource.Outgoing,
                MessageType = message?.MediaURLs.Length > 0 ? MessageType.MMS : MessageType.SMS,
                RawRequest = System.Text.Json.JsonSerializer.Serialize(toForward),
                RawResponse = error
            };

            db.Messages.Add(record);
            await db.SaveChangesAsync();

            // Let the caller know that delivery status for specific numbers.
            return TypedResults.BadRequest(new SendMessageResponse { Message = $"Failed to submit message to FirstPoint. {error}" });
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);

            try
            {
                record = new MessageRecord
                {
                    Id = Guid.NewGuid(),
                    Content = message?.Message ?? string.Empty,
                    DateReceivedUTC = DateTime.UtcNow,
                    From = message?.MSISDN ?? "MSISDN was blank",
                    To = message?.To ?? "To was blank",
                    MediaURLs = string.Empty,
                    MessageSource = MessageSource.Outgoing,
                    MessageType = message?.MediaURLs.Length > 0 ? MessageType.MMS : MessageType.SMS,
                    RawRequest = System.Text.Json.JsonSerializer.Serialize(toForward),
                    RawResponse = ex.Message
                };

                db.Messages.Add(record);
                await db.SaveChangesAsync();
            }
            catch
            {
                // If this throws an exception then there is really nothing we can do as the DB is down and there's nothing to save to.
                Log.Fatal(ex.Message);
            }

            return TypedResults.BadRequest(new SendMessageResponse { Message = $"Failed to submit message to FirstPoint. {ex.Message}" });
        }

    })
        .RequireAuthorization("api").WithOpenApi(x => new(x) { Summary = "Send an SMS or MMS Message.", Description = "Submit outbound messages to this endpoint. The 'to' field is a comma separated list of dialed numbers, or a single dialed number without commas. The 'msisdn' the dialed number of the client that is sending the message. The 'message' field is a string. No validation of the message field occurs before it is forwarded to our upstream vendors. If you include any file paths in the MediaURLs array the message will be sent as an MMS." });

    app.MapPost("1pcom/inbound/MMS", async Task<Results<Ok<string>, BadRequest<string>, Ok<ForwardedMessage>, UnauthorizedHttpResult>> (HttpContext context, string token, MessagingContext db) =>
    {
        if (token != firstPointIncomingToken)
        {
            Log.Warning($"Token is not valid. Token: {token} is not {firstPointIncomingToken}");
            return TypedResults.Unauthorized();
        }

        try
        {
            string msisdn = context.Request.Form["msisdn"].ToString();
            string to = context.Request.Form["to"].ToString();
            string message = context.Request.Form["message"].ToString();
            //string sessionid = context.Request.Form["sessionid"].ToString();
            //string serversecret = context.Request.Form["serversecret"].ToString();
            //string timezone = context.Request.Form["timezone"].ToString();
            //string origtime = context.Request.Form["origtime"].ToString();
            string fullrecipientlist = context.Request.Form["FullRecipientList"].ToString();
            string incomingRequest = string.Join(',', context.Request.Form.Select(x => $"{x.Key}:{x.Value}"));

            // The message field is a JSON object.
            var MMSDescription = System.Text.Json.JsonSerializer.Deserialize<FirstPointMMSMessage>(message);

            // Disabled because this secret value changes whenever.
            //if (serversecret != firstPointIncomingMMSSecret)
            //{
            //    Log.Warning($"Token is not valid. serversecret: {serversecret} is not {firstPointIncomingMMSSecret}");
            //    return TypedResults.Unauthorized();
            //}

            ForwardedMessage toForward = new()
            {
                Content = MMSDescription?.files ?? message,
                MessageSource = MessageSource.Incoming,
                MessageType = MessageType.MMS,
            };

            MessageRecord messageRecord = new MessageRecord
            {
                RawRequest = incomingRequest,
                Content = toForward.Content,
                DateReceivedUTC = DateTime.UtcNow,
                MessageSource = MessageSource.Incoming,
                MessageType = MessageType.MMS,
            };

            if (!string.IsNullOrWhiteSpace(msisdn))
            {
                bool checkFrom = PhoneNumbersNA.PhoneNumber.TryParse(msisdn, out var fromPhoneNumber);
                if (checkFrom && fromPhoneNumber is not null && !string.IsNullOrWhiteSpace(fromPhoneNumber.DialedNumber))
                {
                    if (fromPhoneNumber.Type is not PhoneNumbersNA.NumberType.ShortCode)
                    {
                        toForward.From = $"1{fromPhoneNumber.DialedNumber}";
                    }
                    else
                    {
                        toForward.From = fromPhoneNumber.DialedNumber;
                    }
                }
                else
                {
                    Log.Error($"Failed to parse MSISDN {msisdn} from incoming request {incomingRequest} please file a ticket with the message provider.");
                    // We want the customer to get this message event if the From address is invalid.
                    toForward.Content = $"This message was received from an invalid phone number {msisdn}. You will not be able to respond. {toForward.Content}";
                    toForward.From = "12068588757";
                    msisdn = toForward.From;
                }
            }

            if (!string.IsNullOrWhiteSpace(to))
            {
                List<string> numbers = new();

                foreach (var number in to.Split(','))
                {
                    var checkTo = PhoneNumbersNA.PhoneNumber.TryParse(number, out var toPhoneNumber);

                    if (checkTo && toPhoneNumber is not null)
                    {
                        var formattedNumber = toPhoneNumber.Type is PhoneNumbersNA.NumberType.ShortCode ? $"{toPhoneNumber.DialedNumber}" : $"1{toPhoneNumber.DialedNumber}";
                        // Prevent the duplicates from being included in the the recipients list.
                        if (!numbers.Contains(formattedNumber))
                        {
                            numbers.Add(formattedNumber);
                        }
                    }
                }

                foreach (var number in fullrecipientlist.Split(','))
                {
                    var checkTo = PhoneNumbersNA.PhoneNumber.TryParse(number, out var toPhoneNumber);

                    if (checkTo && toPhoneNumber is not null)
                    {
                        var formattedNumber = toPhoneNumber.Type is PhoneNumbersNA.NumberType.ShortCode ? $"{toPhoneNumber.DialedNumber}" : $"1{toPhoneNumber.DialedNumber}";
                        // Prevent the duplicates from being included in the the recipients list.
                        if (!numbers.Contains(formattedNumber))
                        {
                            numbers.Add(formattedNumber);
                        }
                    }
                }

                // Assume that the first number in the list of To numbers is the primary To that is registered with our service. Treat all others as additional recipients.
                // If multiple numbers in the set of To's are registered as clients the upstream vendor will submit multiple inbound messages so we don't have to handle that scenario.
                if (numbers.Any() && numbers.Count is 1)
                {
                    toForward.To = numbers.FirstOrDefault() ?? string.Empty;
                }
                else if (numbers.Any())
                {
                    toForward.To = numbers.FirstOrDefault() ?? string.Empty;
                    // Dump any extra numbers in the full rec.
                    toForward.AdditionalRecipients = numbers.Where(x => x != toForward.To).ToArray();
                }
                else
                {
                    Log.Error($"Failed to parse To {to} from incoming request {incomingRequest} please file a ticket with the message provider.");

                    messageRecord.Content = toForward.Content;
                    messageRecord.From = toForward.From;
                    messageRecord.RawResponse = $"Failed to parse To {to} from incoming request {incomingRequest} please file a ticket with the message provider.";

                    db.Messages.Add(messageRecord);
                    await db.SaveChangesAsync();

                    return TypedResults.BadRequest($"To {to}{fullrecipientlist} could not be parsed as valid NANP (North American Numbering Plan) numbers. {incomingRequest}");
                }
            }

            List<string> mediaURLs = new();
            string MMSMessagePickupRequest = $"{MMSDescription?.url}&authkey={MMSDescription?.authkey}";
            var spacesConfig = new AmazonS3Config
            {
                ServiceURL = s3ServiceURL,
            };
            using var spacesClient = new AmazonS3Client(digitalOceanSpacesAccessKey, digitalOceanSpacesSecretKey, spacesConfig);
            using var fileUtil = new TransferUtility(spacesClient);

            //string location = app.Configuration.GetValue<string>(WebHostDefaults.ContentRootKey) ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(MMSDescription?.files))
            {
                foreach (string file in MMSDescription.files.Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(file))
                    {
                        string fileDownloadURL = $"{MMSMessagePickupRequest}&file={file}";
                        Log.Information(fileDownloadURL);
                        using var fileStream = await fileDownloadURL.GetStreamAsync();

                        // Save the file to disk rather than S3?!?
                        //var filePath = Path.Combine(location, Path.Combine(toForward.Id.ToString(), file));
                        //using Stream streamToFile = File.Create(filePath);
                        //fileStream.Seek(0, SeekOrigin.Begin);
                        //fileStream.CopyTo(streamToFile);

                        var fileRequest = new TransferUtilityUploadRequest
                        {
                            BucketName = digitalOceanSpacesBucket,
                            InputStream = fileStream,
                            StorageClass = S3StorageClass.Standard,
                            Key = $"{toForward.Id}{file}",
                            CannedACL = S3CannedACL.Private,
                        };
                        await fileUtil.UploadAsync(fileRequest);
                        mediaURLs.Add($"{spacesConfig.ServiceURL}{fileRequest.BucketName}/{fileRequest.Key}");

                        // For debugging in Ops
                        if (file.Contains(".txt"))
                        {
                            messageRecord.Content = await fileDownloadURL.GetStringAsync();
                        }
                    }
                }
            }
            toForward.MediaURLs = mediaURLs.ToArray();
            messageRecord.MediaURLs = string.Join(',', toForward.MediaURLs);

            // We already know that it's good.
            _ = PhoneNumbersNA.PhoneNumber.TryParse(toForward.To, out var toRegisteredNumber);

            var client = await db.ClientRegistrations.FirstOrDefaultAsync(x => x.AsDialed == toRegisteredNumber.DialedNumber);

            if (client is not null && client.AsDialed == toRegisteredNumber.DialedNumber)
            {
                toForward.ClientSecret = client.ClientSecret;
                messageRecord.To = toForward.To;
                messageRecord.From = toForward.From;
                messageRecord.ToForward = System.Text.Json.JsonSerializer.Serialize(toForward);

                try
                {
                    var response = await client.CallbackUrl.PostJsonAsync(toForward);
                    string responseText = await response.GetStringAsync();
                    Log.Information(responseText);
                    Log.Information(System.Text.Json.JsonSerializer.Serialize(toForward));
                    messageRecord.RawResponse = responseText;
                    messageRecord.Succeeded = true;
                }
                catch (FlurlHttpException ex)
                {
                    string error = await ex.GetResponseStringAsync();
                    Log.Error(error);
                    Log.Error(System.Text.Json.JsonSerializer.Serialize(client));
                    Log.Error(System.Text.Json.JsonSerializer.Serialize(toForward));
                    Log.Error($"Failed to forward message to {toForward.To}");
                    messageRecord.RawResponse = $"Failed to forward message to {toForward.To} {error}";
                }

                try
                {
                    await db.Messages.AddAsync(messageRecord);
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return TypedResults.BadRequest($"Failed to save incoming message to the database. {ex.Message} {System.Text.Json.JsonSerializer.Serialize(messageRecord)}");
                }

                Log.Information(System.Text.Json.JsonSerializer.Serialize(messageRecord));
                return TypedResults.Ok("The incoming message was received and forwarded to the client.");
            }
            else
            {
                Log.Warning($"{toForward.To} is not registered as a client.");

                messageRecord.To = toForward.To;
                messageRecord.From = toForward.From;
                messageRecord.ToForward = System.Text.Json.JsonSerializer.Serialize(toForward);
                messageRecord.RawResponse = $"{toForward.To} is not registered as a client.";
                db.Messages.Add(messageRecord);
                await db.SaveChangesAsync();

                return TypedResults.BadRequest($"{toForward.To} is not registered as a client.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.StackTrace ?? "No stack trace found.");
            Log.Information("Failed to read form data by field name.");

            return TypedResults.BadRequest(ex.Message);
        }

    }).WithOpenApi(x => new(x) { Summary = "Receive an MMS Message.", Description = "Submit inbound messages to this endpoint." });

    // When this issue is resolved we can simplify the way that we are receiving data in this endpoint: https://github.com/dotnet/aspnetcore/issues/39430 and https://stackoverflow.com/questions/71047077/net-6-minimal-api-and-multipart-form-data/71048827#71048827
    app.MapPost("/api/inbound/1pcom", async Task<Results<Ok<string>, BadRequest<string>, Ok<ForwardedMessage>, UnauthorizedHttpResult>> (HttpContext context, string token, MessagingContext db) =>
    {
        if (token != firstPointIncomingToken)
        {
            Log.Warning($"Token is not valid. Token: {token} is not {firstPointIncomingToken}");
            return TypedResults.Unauthorized();
        }

        try
        {
            string msisdn = context.Request.Form["msisdn"].ToString();
            string to = context.Request.Form["to"].ToString();
            string message = context.Request.Form["message"].ToString();
            //string sessionid = context.Request.Form["sessionid"].ToString();
            //string serversecret = context.Request.Form["serversecret"].ToString();
            //string timezone = context.Request.Form["timezone"].ToString();
            //string origtime = context.Request.Form["origtime"].ToString();
            string fullrecipientlist = context.Request.Form["FullRecipientList"].ToString();
            string incomingRequest = string.Join(',', context.Request.Form.Select(x => $"{x.Key}:{x.Value}"));

            // Disabled because this secret value changes whenever.
            //if (serversecret != firstPointIncomingSMSSecret)
            //{
            //    Log.Warning($"Token is not valid. serversecret: {serversecret} is not {firstPointIncomingMMSSecret}");
            //    return TypedResults.Unauthorized();
            //}

            ForwardedMessage toForward = new()
            {
                Content = message,
                MessageSource = MessageSource.Incoming,
                MessageType = MessageType.SMS,
            };

            MessageRecord messageRecord = new MessageRecord
            {
                RawRequest = incomingRequest,
                Content = toForward.Content,
                DateReceivedUTC = DateTime.UtcNow,
                MessageSource = MessageSource.Incoming,
                MessageType = MessageType.SMS,
            };

            if (!string.IsNullOrWhiteSpace(msisdn))
            {
                bool checkFrom = PhoneNumbersNA.PhoneNumber.TryParse(msisdn, out var fromPhoneNumber);
                if (checkFrom && fromPhoneNumber is not null && !string.IsNullOrWhiteSpace(fromPhoneNumber.DialedNumber))
                {
                    if (fromPhoneNumber.Type is not PhoneNumbersNA.NumberType.ShortCode)
                    {
                        toForward.From = $"1{fromPhoneNumber.DialedNumber}";
                    }
                    else
                    {
                        toForward.From = fromPhoneNumber.DialedNumber;
                    }
                }
                else
                {
                    Log.Error($"Failed to parse MSISDN {msisdn} from incoming request {incomingRequest} please file a ticket with the message provider.");
                    // We want the customer to get this message event if the From address is invalid.
                    toForward.Content = $"This message was received from an invalid phone number {msisdn}. You will not be able to respond. {toForward.Content}";
                    toForward.From = "12068588757";
                    msisdn = toForward.From;
                }
            }

            if (!string.IsNullOrWhiteSpace(to))
            {
                List<string> numbers = new();

                foreach (var number in to.Split(','))
                {
                    var checkTo = PhoneNumbersNA.PhoneNumber.TryParse(number, out var toPhoneNumber);

                    if (checkTo && toPhoneNumber is not null)
                    {
                        var formattedNumber = toPhoneNumber.Type is PhoneNumbersNA.NumberType.ShortCode ? $"{toPhoneNumber.DialedNumber}" : $"1{toPhoneNumber.DialedNumber}";
                        // Prevent the duplicates from being included in the the recipients list.
                        if (!numbers.Contains(formattedNumber))
                        {
                            numbers.Add(formattedNumber);
                        }
                    }
                }

                foreach (var number in fullrecipientlist.Split(','))
                {
                    var checkTo = PhoneNumbersNA.PhoneNumber.TryParse(number, out var toPhoneNumber);

                    if (checkTo && toPhoneNumber is not null)
                    {
                        var formattedNumber = toPhoneNumber.Type is PhoneNumbersNA.NumberType.ShortCode ? $"{toPhoneNumber.DialedNumber}" : $"1{toPhoneNumber.DialedNumber}";
                        // Prevent the duplicates from being included in the the recipients list.
                        if (!numbers.Contains(formattedNumber))
                        {
                            numbers.Add(formattedNumber);
                        }
                    }
                }

                // Assume that the first number in the list of To numbers is the primary To that is registered with our service. Treat all others as additional recipients.
                // If multiple numbers in the set of To's are registered as clients the upstream vendor will submit multiple inbound messages so we don't have to handle that scenario.
                if (numbers.Any() && numbers.Count is 1)
                {
                    toForward.To = numbers.FirstOrDefault() ?? string.Empty;
                }
                else if (numbers.Any())
                {
                    toForward.To = numbers.FirstOrDefault() ?? string.Empty;
                    // Dump any extra numbers in the full rec.
                    toForward.AdditionalRecipients = numbers.Where(x => x != toForward.To).ToArray();
                }
                else
                {
                    Log.Error($"Failed to parse To {to} from incoming request {incomingRequest} please file a ticket with the message provider.");

                    messageRecord.Content = toForward.Content;
                    messageRecord.From = toForward.From;
                    messageRecord.RawResponse = $"Failed to parse To {to} from incoming request {incomingRequest} please file a ticket with the message provider.";

                    db.Messages.Add(messageRecord);
                    await db.SaveChangesAsync();

                    return TypedResults.BadRequest($"To {to}{fullrecipientlist} could not be parsed as valid NANP (North American Numbering Plan) numbers. {incomingRequest}");
                }
            }

            // We already know that it's good.
            _ = PhoneNumbersNA.PhoneNumber.TryParse(toForward.To, out var toRegisteredNumber);

            var client = await db.ClientRegistrations.FirstOrDefaultAsync(x => x.AsDialed == toRegisteredNumber.DialedNumber);

            if (client is not null && client.AsDialed == toRegisteredNumber.DialedNumber)
            {
                toForward.ClientSecret = client.ClientSecret;
                messageRecord.To = toForward.To;
                messageRecord.From = toForward.From;
                messageRecord.ToForward = System.Text.Json.JsonSerializer.Serialize(toForward);

                try
                {
                    var response = await client.CallbackUrl.PostJsonAsync(toForward);
                    string responseText = await response.GetStringAsync();
                    Log.Information(responseText);
                    Log.Information(System.Text.Json.JsonSerializer.Serialize(toForward));
                    messageRecord.RawResponse = responseText;
                    messageRecord.Succeeded = true;
                }
                catch (FlurlHttpException ex)
                {
                    string error = await ex.GetResponseStringAsync();
                    Log.Error(error);
                    Log.Error(System.Text.Json.JsonSerializer.Serialize(client));
                    Log.Error(System.Text.Json.JsonSerializer.Serialize(toForward));
                    Log.Error($"Failed to forward message to {toForward.To}");
                    messageRecord.RawResponse = $"Failed to forward message to {toForward.To} at {client.CallbackUrl} {ex.StatusCode} {error}";
                }

                try
                {
                    await db.Messages.AddAsync(messageRecord);
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return TypedResults.BadRequest($"Failed to save incoming message to the database. {ex.Message} {System.Text.Json.JsonSerializer.Serialize(messageRecord)}");
                }

                Log.Information(System.Text.Json.JsonSerializer.Serialize(messageRecord));
                return TypedResults.Ok("The incoming message was received and forwarded to the client.");
            }
            else
            {
                Log.Warning($"{toForward.To} is not registered as a client.");

                messageRecord.To = toForward.To;
                messageRecord.From = toForward.From;
                messageRecord.Content = toForward.Content;
                messageRecord.ToForward = System.Text.Json.JsonSerializer.Serialize(toForward);
                messageRecord.RawResponse = $"{toForward.To} is not registered as a client.";
                db.Messages.Add(messageRecord);
                await db.SaveChangesAsync();

                return TypedResults.BadRequest($"{toForward.To} is not registered as a client.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.StackTrace ?? "No stack trace found.");
            Log.Information("Failed to read form data by field name.");
            return TypedResults.BadRequest("Failed to read form data by field name.");
        }
    }).WithOpenApi(x => new(x) { Summary = "For use by First Point Communications only.", Description = "Receives incoming messages from our upstream provider. Forwards valid SMS messages to clients registered through the /client/register endpoint. Forwarded messages are in the form described by the MessageRecord entry in the Schema's section of this page. The is no request body as the data provided by First Point Communications is UrlEncoded like POSTing form data rather than JSON formatted in body of the POST request. The Token is a secret created and maintained by First Point Communications. This endpoint is not for use by anyone other than First Point Communications. It is documented here to help developers understand how incoming messages are forwarded to the client that they have registered with this API. The Messaging.Tests project is a series of functional tests that verify the behavior of this endpoint, because this method of message passing is so chaotic." });

    app.MapPost("/message/forward/test", async Task<Results<Ok<string>, BadRequest<string>>> (ForwardedMessage message, MessagingContext db) =>
    {
        return message.ClientSecret is "thisisatest" && message.From.Length == 11 && message.To.Length > 10
        ? TypedResults.Ok("The incoming message was received and forwarded to the client.")
        : TypedResults.BadRequest("The client secret could not be matched for this number or the numbers were incorrectly formatted.");
    }).WithOpenApi(x => new(x) { Summary = "Endpoint that can be used as the callback URL for a registered client to support functional testing of the message forwarding endpoints.", Description = "For testing purposes only." });

    app.MapPost("/message/send/test", async Task<Results<Ok<FirstPointResponse>, BadRequest<FirstPointResponse>>> (HttpContext context) =>
    {
        string username = context.Request.Form["username"].ToString();
        string password = context.Request.Form["password"].ToString();
        string to = context.Request.Form["to"].ToString();
        string msisdn = context.Request.Form["msisdn"].ToString();
        string messagebody = context.Request.Form["messagebody"].ToString();

        return !string.IsNullOrWhiteSpace(username) && username == firstPointUsername && !string.IsNullOrWhiteSpace(password) && password == firstPointPassword && msisdn.Length == 11 && to.Length > 10
                ? TypedResults.Ok(new FirstPointResponse { Response = new Response { Text = "OK", Code = 200, DeveloperText = "The outbound message was received and the vendor credentials matched." } })
                : TypedResults.BadRequest(new FirstPointResponse { Response = new Response { Text = "BadRequest", Code = 500, DeveloperText = "The outbound message did not include the required credentials to authenticate with the vendor or the numbers were incorrectly formatted." } });
    }).WithOpenApi(x => new(x) { Summary = "Endpoint that can be used to support functional testing of the message sending endpoints without actually sending it to the vendor.", Description = "For testing purposes only." });

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

public static class MessagingClient
{
    public static async Task<Results<Ok<ClientRegistration>, BadRequest<string>, NotFound<string>>> GetByDialedNumberAsync(string asDialed, MessagingContext db, AppSettings appSettings)
    {
        bool checkAsDialed = PhoneNumbersNA.PhoneNumber.TryParse(asDialed, out var asDialedNumber);
        if (!checkAsDialed)
        {
            return TypedResults.BadRequest("AsDialed number couldn't be parsed as a valid NANP (North American Number Plan) number.");
        }

        try
        {
            var registration = await db.ClientRegistrations.FirstOrDefaultAsync(x => x.AsDialed == asDialedNumber.DialedNumber);

            if (registration is not null && !string.IsNullOrWhiteSpace(registration.AsDialed) && registration.AsDialed == asDialedNumber.DialedNumber)
            {
                // At Finns request NANPA numbers are now 1 prefixed to match with the POST client/register endpoint.
                string dialedNumber = asDialedNumber.Type is not PhoneNumbersNA.NumberType.ShortCode ? $"1{asDialedNumber.DialedNumber}" : asDialedNumber.DialedNumber;
                registration.AsDialed = dialedNumber;

                // Verify that this number is routed through our upstream provider.
                var checkRouted = await FirstPointComSMS.GetSMSRoutingByDialedNumberAsync(dialedNumber, appSettings.ConnectionStrings.PComNetUsername, appSettings.ConnectionStrings.PComNetPassword);
                registration.RegisteredUpstream = checkRouted.QueryResult.code is 0 && checkRouted.epid is 265;
                registration.UpstreamStatusDescription = $"{checkRouted?.eptype} {checkRouted?.additional} {checkRouted?.QueryResult.text}".Trim();

                // Don't pollute the registrations with 1 prefixed numbers.
                registration.AsDialed = asDialedNumber.DialedNumber;
                await db.SaveChangesAsync();

                registration.AsDialed = dialedNumber;
                return TypedResults.Ok(registration);
            }
            else
            {
                return TypedResults.NotFound($"Failed to find a client registration for {asDialed}. Parsed as {asDialed}. Please use the client/register endpoint to register a callback URL for this number and enable it for SMS service.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.StackTrace ?? "No stack trace found.");
            return TypedResults.BadRequest(ex.Message);
        }
    }
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

    public class FirstPointResponseMMS
    {
        [JsonPropertyName("0")]
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
    }

    public class FirstPointMMSMessage
    {
        public string authkey { get; set; } = string.Empty;
        public string encoding { get; set; } = string.Empty;
        public string files { get; set; } = string.Empty;
        public string recip { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
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
        public string Content { get; set; } = string.Empty;
        public string MediaURLs { get; set; } = string.Empty;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MessageType MessageType { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MessageSource MessageSource { get; set; }
        // Convert to DateTimeOffset if db is not SQLite.
        [DataType(DataType.DateTime)]
        public DateTime DateReceivedUTC { get; set; } = DateTime.UtcNow;
        public string RawRequest { get; set; } = string.Empty;
        public string RawResponse { get; set; } = string.Empty;
        public bool Succeeded { get; set; } = false;
        public string ToForward { get; set; } = string.Empty;
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
        public MessageSource MessageSource { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime DateReceivedUTC { get; set; } = DateTime.UtcNow;
        [DataType(DataType.Password)]
        public string ClientSecret { get; set; } = string.Empty;

        public MessageRecord ToMessageRecord(string rawRequest, string rawResponse)
        {
            return new()
            {
                Id = Id,
                From = From,
                To = $"{To},{string.Join(',', AdditionalRecipients)}",
                Content = Content,
                MediaURLs = string.Join(',', MediaURLs),
                MessageType = MessageType,
                MessageSource = MessageSource,
                DateReceivedUTC = DateReceivedUTC,
                RawRequest = rawRequest,
                RawResponse = rawResponse
            };
        }
    }

    public class toForwardOutbound
    {
        public string username { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public string to { get; set; } = string.Empty;
        public string msisdn { get; set; } = string.Empty;
        public string messagebody { get; set; } = string.Empty;
    }

    public class UsageSummary
    {
        public string AsDialed { get; set; } = string.Empty;
        public int OutboundMMSCount { get; set; }
        public int InboundMMSCount { get; set; }
        public int OutboundSMSCount { get; set; }
        public int InboundSMSCount { get; set; }
        public string UpstreamStatusDescription { get; set; } = string.Empty;
        public bool RegisteredUpstream { get; set; } = false;
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
        //[DataType(DataType.EmailAddress)]
        //public string Email { get; set; } = string.Empty;
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
        public bool RegisteredUpstream { get; set; } = false;
        public string UpstreamStatusDescription { get; set; } = string.Empty;
        //[DataType(DataType.EmailAddress)]
        //public string Email { get; set; } = string.Empty;
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
        public bool RegisteredUpstream { get; set; } = false;
        public string UpstreamStatusDescription { get; set; } = string.Empty;
        //[DataType(DataType.EmailAddress)]
        //public string Email { get; set; } = string.Empty;
        //public bool EmailVerified { get; set; } = false;
    }

    public class AppSettings
    {
        public Connectionstrings ConnectionStrings { get; set; }

        public class Connectionstrings
        {
            public string PostgresqlProd { get; set; }
            public string BulkVSUsername { get; set; }
            public string BulkVSPassword { get; set; }
            public string TeleAPI { get; set; }
            public string PComNetUsername { get; set; }
            public string PComNetPassword { get; set; }
            public string PComNetIncomingToken { get; set; }
            public string PComNetIncomingMMSSecret { get; set; }
            public string PComNetIncomingSMSSecret { get; set; }
            public string MessagingAPISecret { get; set; }
            public string DOSpacesAccessKey { get; set; }
            public string DOSpacesSecretKey { get; set; }
            public string BucketName { get; set; }
            public string S3ServiceURL { get; set; }
            public string OpsUsername { get; set; }
            public string OpsPassword { get; set; }
        }
    }
}