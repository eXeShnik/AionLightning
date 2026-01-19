using System.IO;
using System.Threading.Tasks;
using AionLightning.Login.Network;
using AionLightning.Login.Network.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AionLightning.Login;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<LoginServer>();
                services.AddSingleton<NetConnector>();
                services.AddSingleton<AionPacketHandlerFactory>();
                services.AddSingleton<GsPacketHandlerFactory>();
            })
            .UseSerilog((hostingContext, loggerConfiguration) =>
            {
                var logPath = Path.Combine("log", "loginserver.log");
                loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day);
            });
}
