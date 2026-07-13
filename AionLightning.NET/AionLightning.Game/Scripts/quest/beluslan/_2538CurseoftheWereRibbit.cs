// Port of Java data/scripts/system/handlers/quest/beluslan/_2538CurseoftheWereRibbit.java.
// Talk to 204827 to start; 790002 (var0->1, gives 182204517 via 204805 at var1->2 first, then
// var2->3 swaps 182204517 for 182204518); talking to 204827 again at var3 flips to REWARD.
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

namespace Quest.Beluslan;

public sealed class _2538CurseoftheWereRibbit : QuestHandlerBase
{
    private const int QuestIdConst = 2538;
    private const int StartNpc = 204827;
    private const int Npc002 = 790002;
    private const int Npc805 = 204805;
    private const int TalismanItem = 182204517;
    private const int CharmedTalismanItem = 182204518;

    private readonly IItemDao _itemDao;

    public _2538CurseoftheWereRibbit(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc002).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Npc805).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 3)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                }
                if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD) return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return false;
            }
            if (targetId == Npc002)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SETPRO3 && var == 2)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, TalismanItem, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 2, 3, reward: false, sameNpc: false,
                        giveItemId: CharmedTalismanItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                }
                return false;
            }
            if (targetId == Npc805)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 2, reward: false, sameNpc: false,
                        giveItemId: TalismanItem, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc) return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
