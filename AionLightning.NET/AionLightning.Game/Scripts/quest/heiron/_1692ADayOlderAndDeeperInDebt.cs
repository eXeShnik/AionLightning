// Port of Java data/scripts/system/handlers/quest/heiron/_1692ADayOlderAndDeeperInDebt.java.
// Single-NPC (798386) dialog + collect-check quest: gather 5x item 2716, turn in.
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

namespace Quest.Heiron;

public sealed class _1692ADayOlderAndDeeperInDebt : QuestHandlerBase
{
    private const int QuestIdConst = 1692;
    private const int Npc          = 798386;

    private readonly IItemDao _itemDao;

    public _1692ADayOlderAndDeeperInDebt(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Npc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (env.TargetId != Npc) return false;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 0, 1, true, 5, 2716, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
