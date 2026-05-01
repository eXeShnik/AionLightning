using System.Net.Sockets;
using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Cs;
using AionLightning.Game.Network.Ls;
using AionLightning.Game.Services;
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
    private readonly IItemDao _itemDao;
    private readonly IQuestDao _questDao;
    private readonly ISocialDao _socialDao;
    private readonly ILegionDao _legionDao;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly GroupService  _groupService;
    private readonly DuelService   _duelService;
    private readonly LegionService _legionService;

    public GsConnectionFactory(ILoggerFactory loggerFactory, GsPacketHandlerFactory factory,
        LsConnectionHolder ls, CsConnectionHolder cs, GameAccountRegistry registry,
        IPlayerDao playerDao, IItemDao itemDao, IQuestDao questDao, ISocialDao socialDao,
        ILegionDao legionDao, GameWorld world, PlayerConnectionRegistry connRegistry,
        GroupService groupService, DuelService duelService, LegionService legionService)
    {
        _loggerFactory = loggerFactory;
        _factory       = factory;
        _ls            = ls;
        _cs            = cs;
        _registry      = registry;
        _playerDao     = playerDao;
        _itemDao       = itemDao;
        _questDao      = questDao;
        _socialDao     = socialDao;
        _legionDao     = legionDao;
        _world         = world;
        _connRegistry  = connRegistry;
        _groupService  = groupService;
        _duelService   = duelService;
        _legionService = legionService;
    }

    public GsClientConnection Create(Socket socket, CancellationToken ct)
        => new(socket, _loggerFactory.CreateLogger<GsClientConnection>(),
               _factory, _ls, _cs, _registry, _playerDao, _itemDao, _questDao, _socialDao, _legionDao, _world, _connRegistry, _groupService, _duelService, _legionService);
}
