using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client abandons a quest. Opcode 0x112.</summary>
public sealed class CM_DELETE_QUEST : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IQuestDao          _questDao;

    private int _questId;

    public CM_DELETE_QUEST(GsClientConnection conn, IQuestDao questDao)
    {
        _conn     = conn;
        _questDao = questDao;
    }

    public override void Read(ref PacketReader r) => _questId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var entry = player.Quests.Get(_questId);
        if (entry is null) return;

        player.Quests.Remove(_questId);
        await _questDao.DeleteAsync(player.ObjectId, _questId, ct);

        await _conn.SendAsync(new SM_QUEST_ACTION(_questId), ct);
        await _conn.SendAsync(new SM_QUEST_LIST(player.Quests.Active), ct);
    }
}
