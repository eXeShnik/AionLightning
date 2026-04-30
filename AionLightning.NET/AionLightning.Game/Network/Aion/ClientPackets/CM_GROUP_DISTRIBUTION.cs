using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Group leader changes loot distribution settings. Opcode 0x10E.</summary>
public sealed class CM_GROUP_DISTRIBUTION : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly GroupService             _groupService;

    private byte _lootDist;
    private byte _lootQuality;

    public CM_GROUP_DISTRIBUTION(GsClientConnection conn, PlayerConnectionRegistry connRegistry,
        GroupService groupService)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
        _groupService = groupService;
    }

    public override void Read(ref PacketReader r)
    {
        _lootDist    = r.ReadC();
        _lootQuality = r.ReadC();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var group = player.Group;
        if (group is null || !group.IsLeader(player.ObjectId)) return;

        group.LootDistribution      = _lootDist;
        group.LootQualityThreshold  = _lootQuality;

        var packet = new SM_GROUP_INFO(group);
        foreach (var member in group.Members)
        {
            var conn = _connRegistry.Get(member.ObjectId);
            if (conn is not null)
                try { await conn.SendAsync(packet, ct); } catch { }
        }
    }
}
