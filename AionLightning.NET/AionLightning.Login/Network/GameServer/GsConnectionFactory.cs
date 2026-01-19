using System.Net.Sockets;
using AionLightning.Commons.Network;
using AionLightning.Login.Network.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network.Gameserver;

public class GsConnectionFactory : IConnectionFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;

    public GsConnectionFactory(ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
    {
        _loggerFactory = loggerFactory;
        _serviceProvider = serviceProvider;
    }

    public AConnection Create(Socket socket, IDispatcher dispatcher)
    {
        var packetHandlerFactory = _serviceProvider.GetRequiredService<GsPacketHandlerFactory>();
        var logger = _loggerFactory.CreateLogger<GsConnection>();
        return new GsConnection(socket, dispatcher, logger, packetHandlerFactory);
    }
}