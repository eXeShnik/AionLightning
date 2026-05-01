using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Group member submits their roll (distributionId=2) or bid (distributionId=3)
/// for a group loot item. Opcode 0x19A.
/// On roll: generates luck (1–100) and broadcasts result to all zone players.
/// Full multi-member coordination (waiting for all to roll) is deferred;
/// the item remains in the loot window and is taken by whoever picks it up last.
/// </summary>
public sealed class CM_GROUP_LOOT : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;

    private int  _groupId;
    private int  _index;
    private int  _itemId;
    private int  _npcObjectId;
    private byte _distributionId;
    private int  _roll;    // 0=pass, 1=rolled
    private long _bid;

    public CM_GROUP_LOOT(GsClientConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _groupId        = r.ReadD();
        _index          = r.ReadD();
        r.ReadD();                   // unk1
        _itemId         = r.ReadD();
        r.ReadC();                   // unk2 (3.0)
        r.ReadC();                   // unk3 (3.5)
        r.ReadC();                   // unk4 (4.6)
        r.ReadC();                   // unk5 (extra)
        _npcObjectId    = r.ReadD();
        _distributionId = (byte)r.ReadC();
        _roll           = r.ReadD();
        _bid            = r.ReadQ();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.Group is null) return;

        int luck    = 0;
        int worldId = player.Position.WorldId;

        switch (_distributionId)
        {
            case 2: // Roll
                luck = _roll == 0 ? 0 : Random.Shared.Next(1, 101);
                break;
            case 3: // Bid — use bid amount as the "luck" value capped to int
                luck = _bid > 0 ? (int)Math.Min(_bid, int.MaxValue) : 0;
                break;
            default:
                return;
        }

        var packet = new SM_GROUP_LOOT(
            _groupId, player.ObjectId, _itemId,
            _npcObjectId, _distributionId, luck, _index);

        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(packet, ct); } catch { }
    }
}
