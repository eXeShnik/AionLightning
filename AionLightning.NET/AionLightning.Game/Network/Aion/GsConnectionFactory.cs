using System.Net.Sockets;
using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Cs;
using AionLightning.Game.Network.Ls;
using Microsoft.Extensions.Logging;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion;

public sealed class GsConnectionFactory : IConnectionFactory<GsClientConnection>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly GsPacketHandlerFactory _factory;
    private readonly LsConnectionHolder _ls;
    private readonly CsConnectionHolder _cs;
    private readonly GameAccountRegistry _registry;
    private readonly IPlayerDao _playerDao;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;

    public GsConnectionFactory(ILoggerFactory loggerFactory, GsPacketHandlerFactory factory,
        LsConnectionHolder ls, CsConnectionHolder cs, GameAccountRegistry registry,
        IPlayerDao playerDao, GameWorld world, PlayerConnectionRegistry connRegistry)
    {
        _loggerFactory = loggerFactory;
        _factory       = factory;
        _ls            = ls;
        _cs            = cs;
        _registry      = registry;
        _playerDao     = playerDao;
        _world         = world;
        _connRegistry  = connRegistry;
    }

    public GsClientConnection Create(Socket socket, CancellationToken ct)
        => new(socket, _loggerFactory.CreateLogger<GsClientConnection>(),
               _factory, _ls, _cs, _registry, _playerDao, _world, _connRegistry);
}
