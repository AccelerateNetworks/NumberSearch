
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NumberSearch.DataAccess.Models;

using ServiceReference;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirstCom
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

                var username = config.GetConnectionString("PComNetUsername");
                var password = config.GetConnectionString("PComNetPassword");
                var gatewayIP = config.GetConnectionString("GatewayIP");

                var postgresSQL = config.GetConnectionString("PostgresqlProd");

                //var results = await NpaNxxFirstPointCom.GetAsync("206", string.Empty, string.Empty, username, password);
                //var x = results.ToArray();

                //var results = await FirstPointComOrderPhoneNumber.PostAsync("2026020612", username, password);

                var numbers = new List<DIDOrderInfo>();


                var results = await FirstPointComOwnedPhoneNumber.GetAllAsync(string.Empty, username, password).ConfigureAwait(false);

                await Task.Delay(1000);
            });
        }
    }
}
