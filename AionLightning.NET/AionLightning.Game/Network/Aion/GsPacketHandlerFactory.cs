using AionLightning.Commons.Events;
using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ClientPackets;
using AionLightning.Game.Network.Cs;
using GameWorld = AionLightning.Game.World.World;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Network.Aion;

public sealed class GsPacketHandlerFactory
{
    private readonly ILogger<GsPacketHandlerFactory> _log;
    private readonly GameServerInfoOptions _gsInfo;
    private readonly NetworkOptions _network;
    private readonly IPlayerDao _playerDao;
    private readonly IPlayerAppearanceDao _appearanceDao;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IEventBus _eventBus;
    private readonly CsConnectionHolder _csHolder;

    public GsPacketHandlerFactory(
        ILogger<GsPacketHandlerFactory> log,
        IOptions<GameServerInfoOptions> gsInfo,
        IOptions<NetworkOptions> network,
        IPlayerDao playerDao,
        IPlayerAppearanceDao appearanceDao,
        GameWorld world,
        PlayerConnectionRegistry connRegistry,
        IEventBus eventBus,
        CsConnectionHolder csHolder)
    {
        _log          = log;
        _gsInfo       = gsInfo.Value;
        _network      = network.Value;
        _playerDao    = playerDao;
        _appearanceDao = appearanceDao;
        _world        = world;
        _connRegistry = connRegistry;
        _eventBus     = eventBus;
        _csHolder     = csHolder;
    }

    public AionClientPacket? Resolve(ushort opcode, GsClientConnection.AionState state, GsClientConnection conn)
    {
        return state switch
        {
            GsClientConnection.AionState.CONNECTED => opcode switch
            {
                0xC2  => new CM_VERSION_CHECK(conn, _gsInfo, _network),
                0xD0  => new CM_TIME_CHECK(conn),
                0x19F => new CM_MAC_ADDRESS(),
                _     => Unknown(state, opcode),
            },
            GsClientConnection.AionState.AUTHED => opcode switch
            {
                0x174 => new CM_CHARACTER_LIST(conn, _playerDao, _appearanceDao),
                0x198 => new CM_MAY_LOGIN_INTO_GAME(conn),
                0xAA  => new CM_ENTER_WORLD(conn, _playerDao, _appearanceDao, _world, _connRegistry),
                0xCE  => new CM_PING(conn),
                0xD0  => new CM_TIME_CHECK(conn),
                0xC1  => new CM_QUIT(conn),
                0x19F => new CM_MAC_ADDRESS(),
                _     => Unknown(state, opcode),
            },
            GsClientConnection.AionState.IN_GAME => opcode switch
            {
                0xAB  => new CM_LEVEL_READY(conn, _world, _connRegistry, _eventBus),
                0xF2  => new CM_MOVE(conn, _world, _connRegistry),
                0x14C => new CM_CHAT_AUTH(conn, _csHolder),
                0xCE  => new CM_PING(conn),
                0xD0  => new CM_TIME_CHECK(conn),
                0xC1  => new CM_QUIT(conn),
                _     => Unknown(state, opcode),
            },
            _ => Unknown(state, opcode),
        };
    }

    private AionClientPacket? Unknown(GsClientConnection.AionState state, ushort opcode)
    {
        _log.LogDebug("Unknown packet: state={State} opcode=0x{Op:X4}", state, opcode);
        return null;
    }
}
