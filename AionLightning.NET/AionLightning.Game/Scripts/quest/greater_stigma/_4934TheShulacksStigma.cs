// Port of Java data/scripts/system/handlers/quest/greater_stigma/_4934TheShulacksStigma.java
// (JIEgOKOJI, modified kecimis). Same shape as _3930SecretoftheShatteredStigma: talk to Vergelmir
// (204051) to start; Moreinen (204211, var 0->1); Teirunerk (204285, var 1->2 shows the collect
// page, then CHECK_USER_HAS_QUEST_ITEM validates item 182207102 and flips to REWARD); the same
// Strongbox npc (700562, var==2) re-broadcasts the quest status after a 3s delay; turn in at
// Vergelmir. See _3930SecretoftheShatteredStigma for the outer-switch fallthrough bug fix (same
// pattern, ported the same way here) and the delayed-update helper.
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

public sealed class _4934TheShulacksStigma : QuestHandlerBase
{
    private const int QuestIdConst  = 4934;
    private const int VergelmirNpc  = 204051;
    private const int MoreinenNpc   = 204211;
    private const int TeirunerkNpc  = 204285;
    private const int StrongboxNpc  = 700562;
    private const int ResearchLogItem = 182207102;

    private readonly IItemDao _itemDao;

    public _4934TheShulacksStigma(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(VergelmirNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(MoreinenNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TeirunerkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StrongboxNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(VergelmirNpc).OnTalk.Add(QuestId);
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
            if (targetId != VergelmirNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != VergelmirNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == MoreinenNpc && var == 0)
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

        if (targetId == TeirunerkNpc)
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
