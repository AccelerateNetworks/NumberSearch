using Microsoft.OpenApi;
using Microsoft.AspNetCore.OpenApi;

using NumberSearch.Mvc.Controllers;
using NumberSearch.Mvc.Models;
using NumberSearch.Mvc.WorkerServices;

using Prometheus;

using Scalar.AspNetCore;

using Serilog;

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;

using ZLinq;

namespace NumberSearch.Mvc
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                builder.AddUserSecrets<Startup>();
            }

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }
        private static readonly string[] middleware = ["Accept-Encoding"];

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            MvcConfiguration mvcConfiguration = new();
            Configuration.Bind("ConnectionStrings", mvcConfiguration);
            services.AddSingleton(mvcConfiguration);

            services.AddDistributedMemoryCache();
            services.AddResponseCaching();
            services.AddOutputCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(3);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            services.AddControllersWithViews();
            //.AddRazorRuntimeCompilation();

            services.AddControllers();

            services.AddRazorPages();

            // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/customize-openapi?view=aspnetcore-10.0
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info = new()
                    {
                        Title = "NumberSearch API",
                        Version = "v1",
                        Description = "To use this API please purchase an access token off of the Services page on our main website. https://acceleratenetworks.com/",
                        Contact = new()
                        {
                            Name = string.Empty,
                            Email = "support@acceleratenetworks.com",
                            Url = new Uri("https://acceleratenetworks.com/"),
                        },
                        License = new()
                        {
                            Name = "Use under LICX",
                            Url = new Uri("https://github.com/AccelerateNetworks/NumberSearch/blob/master/LICENSE"),
                        }
                    };
                    return Task.CompletedTask;
                });
            });

            services.AddSingleton<MonitorLoop>();
            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddApplicationInsightsTelemetry();

            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                    PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                        RateLimitPartition.GetFixedWindowLimiter(GetRemoteHostIpAddressUsingXForwardedFor(httpContext)?.ToString() ?? string.Empty, partition =>
                            new FixedWindowRateLimiterOptions
                            {
                                AutoReplenishment = true,
                                PermitLimit = 240,
                                Window = TimeSpan.FromMinutes(1)
                            })),
                    PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                        RateLimitPartition.GetFixedWindowLimiter(GetRemoteHostIpAddressUsingXForwardedFor(httpContext)?.ToString() ?? string.Empty, partition =>
                            new FixedWindowRateLimiterOptions
                            {
                                AutoReplenishment = true,
                                PermitLimit = 10000,
                                Window = TimeSpan.FromDays(1)
                            })));
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });
        }

        public IPAddress? GetRemoteHostIpAddressUsingXForwardedFor(HttpContext httpContext)
        {
            IPAddress? remoteIpAddress = null;
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .AsValueEnumerable().Select(s => s.Trim());

                foreach (var ip in ips)
                {
                    if (IPAddress.TryParse(ip, out var address) &&
                        (address.AddressFamily is AddressFamily.InterNetwork
                         or AddressFamily.InterNetworkV6))
                    {
                        remoteIpAddress = address;
                        break;
                    }
                }
            }

            remoteIpAddress ??= httpContext.Connection.RemoteIpAddress;

            return remoteIpAddress;
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseSerilogRequestLogging();

            app.UseHttpsRedirection();
            
            // Set cache headers on static files.
            // Disable to prevent caching.
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    // Cache static files for 30 days
                    ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000");
                    ctx.Context.Response.Headers.Append("Expires", DateTime.UtcNow.AddDays(1).ToString("R", CultureInfo.InvariantCulture));
                }
            });
            app.UseRateLimiter();

            app.UseSecurityHeaders(policy => policy
                .AddDefaultSecurityHeaders()
                // Requried to get the embedded YouTube videos to load.
                .AddCrossOriginEmbedderPolicy(x => x.UnsafeNone())
                .AddPermissionsPolicy(builder =>
                {
                    // add all the default versions
                    builder.AddDefaultSecureDirectives();
                    // Allow the autoplay video banner on the homepage to work.
                    builder.AddAutoplay().Self();
                    // Allow the Fullscreen button in the YouTube embedded videos to work.
                    builder.AddFullscreen().Self();
                }));

            app.UseRouting();

            // https://github.com/prometheus-net/prometheus-net
            app.UseHttpMetrics();
            app.UseResponseCaching();
            app.UseOutputCache();
            app.UseStatusCodePagesWithRedirects("/Support");

            app.Use(static async (context, next) =>
            {
                context.Response.GetTypedHeaders().CacheControl =
                    new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromSeconds(10)
                    };
                context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] = middleware;
                await next();
            });

            app.UseAuthorization();

            app.UseSession();

            app.UseMetricServer();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/Number/Search/Bulk", Endpoints.NumberSearchBulkAsync)
                .AddOpenApiOperationTransformer((operation, context, ct) =>
                {
                    operation.Summary = "Lookup a list of dialed numbers";
                    operation.Description = "Get detailed information about a list of North American phone numbers.";
                    return Task.CompletedTask;
                }).CacheOutput();

                endpoints.MapGet("/Client/Numbers", Endpoints.ClientNumbersAsync)
                .AddOpenApiOperationTransformer((operation, context, ct) =>
                {
                    operation.Summary = "Lookup the phone numbers related to your user account";
                    operation.Description = "Get the phone numbers registered as destinations for your user account's domain in FusionPBX. Authenticate using an API key generated by your user account in FusionPBX.";
                    return Task.CompletedTask;
                });

                endpoints.MapGet("/Client/Number/Search", Endpoints.ClientNumberSearchAsync)
                .AddOpenApiOperationTransformer((operation, context, ct) =>
                {
                    operation.Summary = "Lookup a list of dialed numbers related to your user account";
                    operation.Description = "Get detailed information about the North American phone numbers registered as destinations for your user account's domain in FusionPBX. Authenticate using an API key generated by your user account in FusionPBX.";
                    return Task.CompletedTask;
                });

                endpoints.MapGet("/Internet/Providers/Availability​", Endpoints.FCCStateGeoIdLookup)
                .AddOpenApiOperationTransformer((operation, context, ct) =>
                {
                    operation.Summary = "Lookup internet provider availability by state and geo ID";
                    operation.Description = "Get information about internet provider availability in a specific state and geographic area.";
                    return Task.CompletedTask;
                }).CacheOutput();

                endpoints.MapGet("/v2/PhoneNumbers/{PhoneNumber}​", Endpoints.LookupAPIV2)
                .AddOpenApiOperationTransformer((operation, context, ct) =>
                {
                    operation.Summary = "Lookup a phone number";
                    operation.Description = "The Lookup API allows you to query information on a phone number so that you can make a trusted interaction with your user. " +
                    "With this endpoint, you can format and validate phone numbers and add on data packages to get even more in-depth carrier and caller information. ";
                    return Task.CompletedTask;
                }).CacheOutput();

                // Map OpenAPI and Scalar API reference on the endpoint builder
                endpoints.MapOpenApi();
                endpoints.MapScalarApiReference();
                endpoints.MapScalarApiReference("api");

                endpoints.MapDefaultControllerRoute();
                endpoints.MapRazorPages();
                endpoints.MapMetrics();
            });
        }
    }
}
