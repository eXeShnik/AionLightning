using System.Net.Sockets;
using AionLightning.Commons.Network;
using AionLightning.Login.Network.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Aion;

public class AionConnectionFactory : IConnectionFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;

    public AionConnectionFactory(ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
    {
        _loggerFactory = loggerFactory;
        _serviceProvider = serviceProvider;
    }

    public AConnection Create(Socket socket, IDispatcher dispatcher)
    {
        var packetHandlerFactory = _serviceProvider.GetRequiredService<AionPacketHandlerFactory>();
        var logger = _loggerFactory.CreateLogger<LoginConnection>();
        return new LoginConnection(socket, dispatcher, logger, packetHandlerFactory);
    }
}