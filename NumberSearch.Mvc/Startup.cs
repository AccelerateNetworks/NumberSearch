using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;

using NumberSearch.Mvc.Models;
using NumberSearch.Mvc.WorkerServices;

using Prometheus;

using Serilog;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
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

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "NumberSearch API",
                    Description = "An API for looking up numbers from Accelerate Networks.",
                    TermsOfService = new Uri("https://acceleratenetworks.com/privacy"),
                    Contact = new OpenApiContact
                    {
                        Name = "Accelerate Networks",
                        Url = new Uri("https://acceleratenetworks.com/")
                    }
                });

                // using System.Reflection;
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
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

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "NumberSeach API");
                c.EnableTryItOutByDefault();
                c.EnableValidator();
                c.DisplayRequestDuration();
            });

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
                endpoints.MapDefaultControllerRoute();
                endpoints.MapRazorPages();
                endpoints.MapMetrics();
            });
        }
    }
}
