using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.Linq;
using System.Threading.Tasks;

namespace BulkVS
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                var config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddUserSecrets("40f816f3-0a65-4523-a9be-4bbef0716720")
                    .Build();

                var apikey = config.GetConnectionString("BulkVSAPIKEY");
                var apisecret = config.GetConnectionString("BulkVSAPISecret");

                var postgresSQL = config.GetConnectionString("PostgresqlProd");

                var x = await NpaBulkVS.GetAsync("206", apikey, apisecret).ConfigureAwait(false);

                var BulkVSStats = await MainBulkVS.IngestPhoneNumbersAsync(apikey, apisecret, postgresSQL);

                await BulkVSStats.PostAsync(postgresSQL);
            });
        }
    }
}

