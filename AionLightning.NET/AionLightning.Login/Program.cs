using AionLightning.Login.Network;
using AionLightning.Login.Network.Factories;
using AionLightning.LoginServer.Network.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AionLightning.Login;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host =
            Host.CreateDefaultBuilder(args)
                .UseSerilog((hostingContext, loggerConfiguration) =>
                {
                    var logPath = Path.Combine("log", "loginserver.log");
                    loggerConfiguration
                        .ReadFrom.Configuration(hostingContext.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Console()
                        .WriteTo.File(logPath, rollingInterval: RollingInterval.Day);
                })
                .ConfigureServices((_, services) =>
                {
                    services.AddHostedService<LoginServer>();
                    services.AddSingleton<NetConnector>();
                    services.AddSingleton<AionPacketHandlerFactory>();
                    services.AddSingleton<GsPacketHandlerFactory>();
                })
                .Build();
        await host.RunAsync();
    }
}