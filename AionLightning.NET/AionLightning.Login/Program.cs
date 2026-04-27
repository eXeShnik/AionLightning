using AionLightning.Commons.Configuration;
using AionLightning.Commons.Database;
using AionLightning.Commons.Hosting;
using AionLightning.Login;
using AionLightning.Login.Configs.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = AionHostBuilder.CreateAion(args, "AionLogin");

builder.Services
    .AddAionOptions<NetworkOptions>("LoginServer:Network")
    .AddAionOptions<SecurityOptions>("LoginServer:Security")
    .AddAionOptions<AccountsOptions>("LoginServer:Accounts")
    .AddAionOptions<MaintenanceOptions>("LoginServer:Maintenance")
    .AddAionOptions<PingPongOptions>("LoginServer:PingPong");

builder.Services.AddAionDataSource(builder.Configuration, "LoginDb");

// SchemaMigrationHost runs first (registration order), SmokeHost runs second.
builder.Services.AddHostedService<SchemaMigrationHost>();
builder.Services.AddHostedService<SmokeHost>();

await builder.Build().RunAsync();
