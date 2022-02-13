using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NpgsqlTypes;

using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;

using System;
using System.Collections.Generic;

namespace NumberSearch.Mvc
{
    public partial class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
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

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            IDictionary<string, ColumnWriterBase> columnWriters = new Dictionary<string, ColumnWriterBase>
            {
                { "message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
                { "message_template", new MessageTemplateColumnWriter(NpgsqlDbType.Text) },
                { "level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
                { "raise_date", new TimestampColumnWriter(NpgsqlDbType.TimestampTz) },
                { "exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
                { "properties", new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb) },
                { "props_test", new PropertiesColumnWriter(NpgsqlDbType.Jsonb) },
                { "machine_name", new SinglePropertyColumnWriter("MachineName", PropertyWriteMethod.ToString, NpgsqlDbType.Text, "l") }
            };

            return Host.CreateDefaultBuilder(args)
                            .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
                                .ReadFrom.Configuration(hostingContext.Configuration)
                                .Enrich.FromLogContext()
                                .WriteTo.File($"NumberSearch.Mvc_{DateTime.Now:yyyyMMdd}.txt",
                                                rollingInterval: RollingInterval.Day,
                                                rollOnFileSizeLimit: true)
                                .WriteTo.PostgreSQL(hostingContext.Configuration.GetConnectionString("PostgresqlProd"),
                                "Mvc",
                                columnWriters,
                                useCopy: true,
                                needAutoCreateSchema: true,
                                needAutoCreateTable: true,
                                schemaName: "Logs",
                                period: new TimeSpan(0, 0, 30)))
                                .ConfigureWebHostDefaults(webBuilder =>
                                {
                                    webBuilder.UseStartup<Startup>();
                                });
        }
    }
}
