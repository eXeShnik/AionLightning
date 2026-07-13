// Port of Java data/scripts/system/handlers/quest/danuar_sanctuary/_26980BuriedWithTheirTreasure.java
// Simple talk quest: accept + turn in at Dintanum (801954).
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

namespace Quest.DanuarSanctuary;

public sealed class _26980BuriedWithTheirTreasure : QuestHandlerBase
{
    private const int QuestIdConst = 26980;
    private const int Npc          = 801954; // Dintanum.

    public _26980BuriedWithTheirTreasure(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(Npc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != Npc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != Npc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await DefaultCloseDialogAsync(env, conn, 0, 0, reward: true, sameNpc: true, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == Npc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
