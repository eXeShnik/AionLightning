using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AionLightning.Commons.Hosting;

public static class AionHostBuilder
{
    public static HostApplicationBuilder CreateAion(string[] args, string appName)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSerilog((sp, lc) => lc
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(sp)
            .Enrich.FromLogContext());

        if (OperatingSystem.IsWindows())
        {
            builder.Services.AddWindowsService(o => o.ServiceName = appName);
        }

        return builder;
    }
}
