using AionLightning.Commons.Network;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client shares a quest with group members. Opcode 0x146.</summary>
public sealed class CM_QUEST_SHARE : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IDataManager             _dataManager;

    private int _questId;

    public CM_QUEST_SHARE(GsClientConnection conn, PlayerConnectionRegistry connRegistry, IDataManager dataManager)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
        _dataManager  = dataManager;
    }

    public override void Read(ref PacketReader r) => _questId = r.ReadD();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var group = player.Group;
        if (group is null) return;

        var template = _dataManager.Quests.GetTemplate(_questId);
        if (template is null || template.CannotShare) return;

        // Sharer must have the quest active (not completed)
        var myEntry = player.Quests.Active.FirstOrDefault(q => q.QuestId == _questId);
        if (myEntry is null) return;

        var sharePkt = new SM_QUEST_ACTION(_questId, player.ObjectId);

        foreach (var member in group.Members)
        {
            if (member.ObjectId == player.ObjectId) continue;

            var mc = _connRegistry.Get(member.ObjectId);
            if (mc?.ActivePlayer is not { } memberPlayer) continue;

            // Skip if member already has the quest started or finished
            bool alreadyActive = memberPlayer.Quests.Active.Any(q => q.QuestId == _questId);
            bool alreadyDone   = memberPlayer.Quests.Completed.Any(q => q.QuestId == _questId);
            if (alreadyActive || alreadyDone) continue;

            // Level gate
            if (memberPlayer.Level < template.MinLevel) continue;

            // Distance check — 25 units (matches Java GROUP_MAX_DISTANCE default)
            if (player.Position.DistanceTo(memberPlayer.Position) > 25f) continue;

            try { await mc.SendAsync(sharePkt, ct); } catch { }
        }
    }
}
