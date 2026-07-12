// Port of Java data/scripts/system/handlers/quest/greater_stigma/_30317GroupSpiritsandStigmaSlots.java
// (Gigi). Same shape as _30217GroupStigmasScars: talk to 799208 to start; at 799322, SETPRO1 (or
// QUEST_SELECT once var != 0) spawns 799506 at the player's position and advances var 0->1; talk
// to the spawned 799506 (SETPRO2, or QUEST_SELECT once var != 1) advances var 1->2; back at
// 799208, handing in both collect items (182209718/182209719) flips to REWARD; turn in at 799208.
// Java bug fixed (same as _30217GroupStigmasScars): the outer switch(targetId) has no break
// between case blocks, causing accidental cross-npc fallthrough — ported as independent per-npc
// `if` blocks. Inner per-npc QUEST_SELECT -> SETPROx fallthroughs are intentional and ported via
// explicit ORs (same precedent as pandaemonium/_4966GrowthNinissFirstCharm).
// Skip vs Java: the spawned 799506's `getController().onDelete()` despawn-on-talk is omitted (no
// Npc AI/controller subsystem in this port yet) — the underlying var transition still applies.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.GreaterStigma;

public sealed class _30317GroupSpiritsandStigmaSlots : QuestHandlerBase
{
    private const int QuestIdConst = 30317;
    private const int StartNpc     = 799208;
    private const int TriggerNpc   = 799322;
    private const int SpawnedNpc   = 799506;
    private const int Item1Id      = 182209718;
    private const int Item2Id      = 182209719;

    private readonly IItemDao _itemDao;

    public _30317GroupSpiritsandStigmaSlots(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SpawnedNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TriggerNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status != QuestStatus.START)
        {
            if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
            {
                if (env.DialogId == (int)DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        int var = entry.GetVar(0);

        if (targetId == TriggerNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1 || (dialog == DialogAction.QUEST_SELECT && var != 0))
            {
                var pos = player.Position;
                SpawnQuestNpc(pos.WorldId, pos.InstanceId, SpawnedNpc, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
                entry.SetVar(0, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return false;
        }

        if (targetId == StartNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
            {
                bool has1 = (player.Inventory.FindByItemId(Item1Id)?.Count ?? 0) > 0;
                bool has2 = (player.Inventory.FindByItemId(Item2Id)?.Count ?? 0) > 0;
                if (has1 && has2)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, Item1Id, 1, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, Item2Id, 1, ct);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                }
                return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
            }
            return false;
        }

        if (targetId == SpawnedNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2 || (dialog == DialogAction.QUEST_SELECT && var != 1))
            {
                entry.SetVar(0, 2);
                await UpdateQuestStatusAsync(conn, entry, ct);
                return true;
            }
            return false;
        }

        return false;
    }
}
