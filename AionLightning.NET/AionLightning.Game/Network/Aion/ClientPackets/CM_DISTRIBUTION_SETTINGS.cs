using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Group leader sets loot distribution rules. Opcode 0x19B.</summary>
public sealed class CM_DISTRIBUTION_SETTINGS : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly GroupService             _groupService;

    private int _lootRule;
    private int _lootMisc;
    private int _commonItemAbove;
    private int _superiorItemAbove;
    private int _heroicItemAbove;
    private int _fabledItemAbove;
    private int _ethernalItemAbove;
    private int _autoDistribution;

    public CM_DISTRIBUTION_SETTINGS(GsClientConnection conn,
        PlayerConnectionRegistry connRegistry, GroupService groupService)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
        _groupService = groupService;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadD();                        // unk1
        _lootRule         = r.ReadD();    // 0=ffa, 1=round-robin, 2=leader
        _lootMisc         = r.ReadD();
        _commonItemAbove  = r.ReadD();
        _superiorItemAbove = r.ReadD();
        _heroicItemAbove  = r.ReadD();
        _fabledItemAbove  = r.ReadD();
        _ethernalItemAbove = r.ReadD();
        _autoDistribution = r.ReadD();
        r.ReadD();                        // unk2
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var group = player.Group;
        if (group is null || !group.IsLeader(player.ObjectId)) return;

        group.LootDistribution  = _lootRule;
        group.LootMisc          = _lootMisc;
        group.CommonItemAbove   = _commonItemAbove;
        group.SuperiorItemAbove = _superiorItemAbove;
        group.HeroicItemAbove   = _heroicItemAbove;
        group.FabledItemAbove   = _fabledItemAbove;
        group.EthernalItemAbove = _ethernalItemAbove;
        group.AutoDistribution  = _autoDistribution;

        var info = new SM_GROUP_INFO(group);
        foreach (var member in group.Members)
        {
            var mc = _connRegistry.Get(member.ObjectId);
            if (mc is not null)
                try { await mc.SendAsync(info, ct); } catch { }
        }
    }
}
