using System.Net;
using AionLightning.Commons.Network;
using AionLightning.Login.Configs;
using AionLightning.Login.Network.Aion;
using AionLightning.Login.Network.Gameserver;
using Microsoft.Extensions.Logging;

namespace AionLightning.Login.Network;

public class NetConnector
{
    private readonly ILogger<NetConnector> _logger;
    private readonly NioServer _nioServer;

    public NetConnector(ILogger<NetConnector> logger, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
    {
        _logger = logger;

        var aionConnectionFactory = new AionConnectionFactory(loggerFactory, serviceProvider);
        var gsConnectionFactory = new GsConnectionFactory(loggerFactory, serviceProvider);

        var aionCfg = new ServerCfg(IPAddress.Parse(Config.LoginBindAddress), Config.LoginPort, "Aion Connections", aionConnectionFactory);
        var gsCfg = new ServerCfg(IPAddress.Parse(Config.GameBindAddress), Config.GamePort, "Gs Connections", gsConnectionFactory);

        _nioServer = new NioServer(Config.NioReadThreads, loggerFactory, gsCfg, aionCfg);
    }

    public void Connect()
    {
        _nioServer.Connect();
    }
}