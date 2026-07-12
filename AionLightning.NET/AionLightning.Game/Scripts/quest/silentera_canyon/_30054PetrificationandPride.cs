// Port of Java data/scripts/system/handlers/quest/silentera_canyon/_30054PetrificationandPride.java (Ritsu).
// Talk to Gellius (798929) to start; at Telemachus (203901) SETPRO1 advances var 0 to 1; back at
// Gellius, QUEST_SELECT at var 1 shows dialog 2375 and SELECT_QUEST_REWARD flips straight to REWARD
// and finishes the turn-in on the same npc (Java calls sendQuestEndDialog right after setting
// REWARD, relying on its own SELECT_QUEST_REWARD/REWARD-status guard - same guard as
// SendQuestEndDialogAsync here).
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

namespace Quest.SilenteraCanyon;

public sealed class _30054PetrificationandPride : QuestHandlerBase
{
    private const int QuestIdConst = 30054;
    private const int GelliusNpc   = 798929;
    private const int TelemachusNpc = 203901;

    public _30054PetrificationandPride(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GelliusNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(GelliusNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TelemachusNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry       = env.Player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != GelliusNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != GelliusNpc) return false;
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId == GelliusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD && var == 1)
                    return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: true, ct);
            }
            else if (targetId == TelemachusNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1 && var == 0)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
        }

        return false;
    }
}
