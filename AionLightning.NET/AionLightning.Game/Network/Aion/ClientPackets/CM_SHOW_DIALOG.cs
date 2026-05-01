using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests NPC dialog. Opcode 0x116.</summary>
public sealed class CM_SHOW_DIALOG : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly GameWorld          _world;
    private readonly IDataManager       _dataManager;
    private readonly IPlayerDao         _playerDao;

    private int _targetObjectId;

    public CM_SHOW_DIALOG(GsClientConnection conn, GameWorld world,
        IDataManager dataManager, IPlayerDao playerDao)
    {
        _conn        = conn;
        _world       = world;
        _dataManager = dataManager;
        _playerDao   = playerDao;
    }

    public override void Read(ref PacketReader r) => _targetObjectId = r.ReadD();

    private const float MaxInteractRange = 10.0f;

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        // Private store: if the target is a player with an open shop, send the store listing
        var storeOwner = _world.GetPlayerByObjectId(_targetObjectId);
        if (storeOwner is not null)
        {
            if ((storeOwner.State & CreatureState.PrivateShop) != 0)
                await _conn.SendAsync(new SM_PRIVATE_STORE(storeOwner), ct);
            return;
        }

        var npc = _world.GetNpcByObjectId(_targetObjectId);
        if (npc is null) return;
        if (player.Position.DistanceTo(npc.Position) > MaxInteractRange) return;

        if (string.Equals(npc.Template.NpcType, "BINDSTONE", StringComparison.OrdinalIgnoreCase))
        {
            player.BindPosition = npc.Position;
            await _playerDao.UpdateBindPointAsync(player.ObjectId, npc.Position, ct);
            await _conn.SendAsync(new SM_BIND_POINT_INFO(npc.Position), ct);
            return;
        }

        // dialogId 10 = standard NPC greeting (shows Buy/Sell/Quest buttons depending on NPC type)
        await _conn.SendAsync(new SM_DIALOG_WINDOW(_targetObjectId, dialogId: 10, questId: 0), ct);
    }
}
