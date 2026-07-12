// Port of Java data/scripts/system/handlers/quest/greater_stigma/_3930SecretoftheShatteredStigma.java
// (JIEgOKOJI, modified kecimis). Talk to Miriya (203711) to start; Xenophon (203833, var 0->1);
// Koruchinerk (798321, var 1->2 shows the collect page, then CHECK_USER_HAS_QUEST_ITEM validates
// item 182206075 and flips to REWARD); Strongbox (700562, var==2) just re-broadcasts the quest
// status after a 3s delay (Java ThreadPoolManager.schedule, ported as a fire-and-forget
// Task.Delay); turn in at Miriya.
// Java bug fixed: the outer switch(targetId) in onDialogEvent has no break after the 203833 case,
// so at var != 0 (or an unmatched dialog at var == 0) it silently falls through into the 798321
// case body using the same dialog value, even though the target is really Xenophon, not
// Koruchinerk. Ported as independent per-npc `if` blocks — no cross-npc fallthrough. (798321's own
// case already ends with `return false;` in Java, so it doesn't leak into 700562 — only the
// 203833 -> 798321 leak needed fixing.)
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

public sealed class _3930SecretoftheShatteredStigma : QuestHandlerBase
{
    private const int QuestIdConst  = 3930;
    private const int MiriyaNpc     = 203711;
    private const int XenophonNpc   = 203833;
    private const int KoruchinerkNpc = 798321;
    private const int StrongboxNpc  = 700562;
    private const int ResearchLogItem = 182206075;

    private readonly IItemDao _itemDao;

    public _3930SecretoftheShatteredStigma(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(MiriyaNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(XenophonNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KoruchinerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StrongboxNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MiriyaNpc).OnTalk.Add(QuestId);
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
            if (targetId != MiriyaNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != MiriyaNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == XenophonNpc && var == 0)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
            {
                entry.SetVar(0, 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            }
            return false;
        }

        if (targetId == KoruchinerkNpc)
        {
            if (var == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    entry.SetVar(0, 2);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                    return true;
                }
            }
            else if (var == 2)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (env.DialogId == (int)DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    if ((player.Inventory.FindByItemId(ResearchLogItem)?.Count ?? 0) < 1)
                        return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, ResearchLogItem, 1, ct);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 10000, ct);
                }
            }
            return false;
        }

        if (targetId == StrongboxNpc && var == 2)
        {
            _ = DelayedUpdateStatusAsync(conn, entry);
            return true;
        }

        return false;
    }

    private async Task DelayedUpdateStatusAsync(GsClientConnection conn, QuestEntry entry)
    {
        await Task.Delay(3000);
        try { await UpdateQuestStatusAsync(conn, entry, CancellationToken.None); } catch { }
    }
}
