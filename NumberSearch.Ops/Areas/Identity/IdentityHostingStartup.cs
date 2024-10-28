using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(NumberSearch.Ops.Areas.Identity.IdentityHostingStartup))]
namespace NumberSearch.Ops.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
            });
        }
    }
}