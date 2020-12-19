using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Events;

using System;

namespace NumberSearch.Mvc
{
    public class Program
    {
        public static int Main(string[] args)
        {

            Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Debug()
                            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                            .Enrich.FromLogContext()
                            .WriteTo.Async(a => a.Console())
                            .CreateLogger();

            try
            {
                Log.Information("Starting web host");
                var host = CreateHostBuilder(args).Build();

                // Background tasks
                var monitorLoop = host.Services.GetRequiredService<MonitorLoop>();
                monitorLoop.StartMonitorLoop();

                host.Run();

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Async(a => a.Console())
                    .WriteTo.Async(a => a.File($"NumberSearch.Mvc_{DateTime.Now:yyyyMMdd}.txt",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true))
                    .WriteTo.Async(a => a.Debug())
                    .WriteTo.Async(a => a.ApplicationInsights(TelemetryConfiguration.Active, TelemetryConverter.Traces)))
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
