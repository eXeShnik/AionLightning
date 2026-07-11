// Port of Java data/scripts/system/handlers/quest/eltnen/_1367MabangtahsFeast.java (Atomics).
// Single-NPC (204023) quest: 3 independent ingredient-count checks (items are never consumed,
// faithful to Java), any one of which flips the quest straight to REWARD with its own dialog page.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Eltnen;

public sealed class _1367MabangtahsFeast : QuestHandlerBase
{
    private const int QuestIdConst = 1367;
    private const int NpcId        = 204023;
    private const int HamItemId    = 182201333;
    private const int WineItemId   = 182201332;
    private const int CheeseItemId = 182201331;

    public _1367MabangtahsFeast(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(NpcId).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(NpcId).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        if (env.TargetId != NpcId) return false;
        var entry = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
            {
                long ham = player.Inventory.FindByItemId(HamItemId)?.Count ?? 0;
                long wine = player.Inventory.FindByItemId(WineItemId)?.Count ?? 0;
                long cheese = player.Inventory.FindByItemId(CheeseItemId)?.Count ?? 0;
                return ham > 1 || wine > 5 || cheese > 0
                    ? await SendQuestDialogAsync(conn, targetObjId, 1352, ct)
                    : await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            }
            if (dialog == DialogAction.SETPRO1)
            {
                long cheese = player.Inventory.FindByItemId(CheeseItemId)?.Count ?? 0;
                if (cheese > 0)
                {
                    entry.SetVar(0, 1);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            }
            if (dialog == DialogAction.SETPRO2)
            {
                long wine = player.Inventory.FindByItemId(WineItemId)?.Count ?? 0;
                if (wine > 4)
                {
                    entry.SetVar(0, 2);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 6, ct);
                }
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            }
            if (dialog == DialogAction.SETPRO3)
            {
                long ham = player.Inventory.FindByItemId(HamItemId)?.Count ?? 0;
                if (ham > 1)
                {
                    entry.SetVar(0, 3);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 7, ct);
                }
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
