// Port of Java data/scripts/system/handlers/quest/greater_stigma/_30217GroupStigmasScars.java (Gigi).
// Talk to 798909 to start; at 798941, SETPRO1 (or QUEST_SELECT once var != 0) spawns 799506 at the
// player's position (Batch 0.2 SpawnQuestNpc) and advances var 0->1; talk to the spawned 799506
// (SETPRO2, or QUEST_SELECT once var != 1) advances var 1->2; back at 798909, handing in both
// collect items (182209618/182209619) flips to REWARD; turn in at 798909.
// Java bug fixed: the outer switch(targetId) in onDialogEvent has no break between the 798941/
// 798909/799506 case blocks, so a non-matching dialog at one npc silently falls through into the
// next npc's case body (e.g. any unhandled dialog at 798941 would also run 798909's QUEST_SELECT
// check using the still-current dialog value). Ported as independent per-npc `if` blocks instead
// — no cross-npc fallthrough. The inner per-npc switch fallthroughs (QUEST_SELECT -> SETPROx once
// var has already advanced) are intentional Java shortcuts and are ported via explicit ORs, same
// precedent as pandaemonium/_4966GrowthNinissFirstCharm.
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

public sealed class _30217GroupStigmasScars : QuestHandlerBase
{
    private const int QuestIdConst = 30217;
    private const int StartNpc     = 798909;
    private const int TriggerNpc   = 798941;
    private const int SpawnedNpc   = 799506;
    private const int Item1Id      = 182209618;
    private const int Item2Id      = 182209619;

    private readonly IItemDao _itemDao;

    public _30217GroupStigmasScars(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
