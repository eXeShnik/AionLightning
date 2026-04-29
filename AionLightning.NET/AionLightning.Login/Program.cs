using AionLightning.Commons.Configuration;
using AionLightning.Commons.Database;
using AionLightning.Commons.Hosting;
using AionLightning.Commons.Network;
using AionLightning.Login;
using AionLightning.Login.Configs.Options;
using AionLightning.Login.Controller;
using AionLightning.Login.Dao;
using AionLightning.Login.Network.Aion;
using AionLightning.Login.Network.Factories;
using AionLightning.Login.Network.GameServer;
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

// DAOs
builder.Services.AddSingleton<IAccountDao, AccountDaoImpl>();
builder.Services.AddSingleton<IBannedIpDao, BannedIpDaoImpl>();

// Controllers
builder.Services.AddSingleton<BannedIpController>();
builder.Services.AddSingleton<IAccountController, AccountController>();

// Aion-client networking
builder.Services.AddSingleton<AionPacketHandlerFactory>();
builder.Services.AddSingleton<IConnectionFactory<LoginConnection>, AionConnectionFactory>();

// GS networking
builder.Services.AddSingleton<GsPacketHandlerFactory>();
builder.Services.AddSingleton<IConnectionFactory<GsConnection>, GsConnectionFactory>();

// Background services: migration → login server → GS ping
builder.Services.AddHostedService<SchemaMigrationHost>();
builder.Services.AddHostedService<LoginServerHost>();
builder.Services.AddHostedService<GsPingService>();

await builder.Build().RunAsync();
