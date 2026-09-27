using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.Benchmarkemail.Services;
using Nop.Web.Framework.Infrastructure.Extensions;

namespace Nop.Plugin.Misc.Benchmarkemail.Infrastructure;

public class PluginNopStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<BenchmarkEmailHttpClient>().WithProxy();
        services.AddScoped<BenchmarkEmailManager>();
    }

    public void Configure(IApplicationBuilder application)
    {
    }

    public int Order => 3000;
}