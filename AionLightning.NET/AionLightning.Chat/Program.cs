using AionLightning.Chat;
using AionLightning.Chat.Configs.Options;
using AionLightning.Chat.Model;
using AionLightning.Chat.Network.Aion;
using AionLightning.Chat.Network.Gs;
using AionLightning.Chat.Service;
using AionLightning.Commons.Configuration;
using AionLightning.Commons.Hosting;
using AionLightning.Commons.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = AionHostBuilder.CreateAion(args, "AionChat");

// Options
builder.Services
    .AddAionOptions<ChatNetworkOptions>("ChatServer:Network")
    .AddAionOptions<ChatAuthOptions>("ChatServer:Auth");

// Model
builder.Services.AddSingleton<ChatChannels>();

// Service
builder.Services.AddSingleton<ChatService>();

// GS network
builder.Services.AddSingleton<GsPacketHandlerFactory>();
builder.Services.AddSingleton<IConnectionFactory<GsConnection>, GsConnectionFactory>();

// Aion client network
builder.Services.AddSingleton<AionPacketHandlerFactory>();
builder.Services.AddSingleton<IConnectionFactory<AionClientConnection>, AionConnectionFactory>();

// Hosted service
builder.Services.AddHostedService<ChatServerHost>();

await builder.Build().RunAsync();
