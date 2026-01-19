using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Hello, World!");

await host.RunAsync();
