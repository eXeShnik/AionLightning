using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// Group/alliance leader sets loot distribution rules. Opcode 0x19B.
/// Java applies this to both the player's group and alliance when either is present.
/// </summary>
public sealed class CM_DISTRIBUTION_SETTINGS : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly GroupService             _groupService;
    private readonly AllianceService          _allianceService;

    private int _lootRule;
    private int _lootMisc;
    private int _commonItemAbove;
    private int _superiorItemAbove;
    private int _heroicItemAbove;
    private int _fabledItemAbove;
    private int _ethernalItemAbove;
    private int _autoDistribution;

    public CM_DISTRIBUTION_SETTINGS(GsClientConnection conn,
        PlayerConnectionRegistry connRegistry, GroupService groupService, AllianceService allianceService)
    {
        _conn             = conn;
        _connRegistry     = connRegistry;
        _groupService     = groupService;
        _allianceService  = allianceService;
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
        if (group is not null && group.IsLeader(player.ObjectId))
        {
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

        var alliance = player.Alliance;
        if (alliance is not null && alliance.IsLeader(player.ObjectId))
        {
            alliance.LootDistribution  = _lootRule;
            alliance.LootMisc          = _lootMisc;
            alliance.CommonItemAbove   = _commonItemAbove;
            alliance.SuperiorItemAbove = _superiorItemAbove;
            alliance.HeroicItemAbove   = _heroicItemAbove;
            alliance.FabledItemAbove   = _fabledItemAbove;
            alliance.EthernalItemAbove = _ethernalItemAbove;
            alliance.AutoDistribution  = _autoDistribution;

            var allianceInfo = new SM_ALLIANCE_INFO(alliance);
            foreach (var member in alliance.Members)
            {
                var mc = _connRegistry.Get(member.ObjectId);
                if (mc is not null)
                    try { await mc.SendAsync(allianceInfo, ct); } catch { }
            }
        }
    }
}
