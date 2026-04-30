using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client opens the loot window on a dead NPC. Opcode 0x178.</summary>
public sealed class CM_START_LOOT : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly LootService _lootService;

    private int _targetObjectId;

    public CM_START_LOOT(GsClientConnection conn, LootService lootService)
    {
        _conn        = conn;
        _lootService = lootService;
    }

    public override void Read(ref PacketReader r)
    {
        _targetObjectId = r.ReadD();
        r.ReadC(); // action byte
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        if (_conn.ActivePlayer is null) return;

        await _conn.SendAsync(new SM_LOOT_STATUS(_targetObjectId, SM_LOOT_STATUS.State.Open), ct);

        var pending = _lootService.GetLoot(_targetObjectId);
        if (pending is null || pending.Count == 0)
        {
            await _conn.SendAsync(new SM_LOOT_ITEMLIST(_targetObjectId, Array.Empty<SM_LOOT_ITEMLIST.LootItem>()), ct);
            return;
        }

        var lootItems = pending
            .Select(e => new SM_LOOT_ITEMLIST.LootItem(e.ItemId, e.Count, e.IsTradeable))
            .ToArray();

        await _conn.SendAsync(new SM_LOOT_ITEMLIST(_targetObjectId, lootItems), ct);
    }
}
