// Port of Java data/scripts/system/handlers/quest/pandaemonium/_2921LoveAtFirstSight.java.
// Accept default at 204261; 3-step chain (204256 var0 0->1, 204192 var0 1->2, 204130 var0 2->3);
// back at 204261, SELECT_QUEST_REWARD closes in place (sameNpc) once var0==3; turn in at 204261.
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

namespace Quest.Pandaemonium;

public sealed class _2921LoveAtFirstSight : QuestHandlerBase
{
    private const int QuestIdConst = 2921;
    private const int StartNpc = 204261;
    private const int StepOneNpc = 204256;
    private const int StepTwoNpc = 204192;
    private const int StepThreeNpc = 204130;

    public _2921LoveAtFirstSight(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StepOneNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StepTwoNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StepThreeNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == StepOneNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return entry.GetVar(0) == 0 && await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == StepTwoNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return entry.GetVar(0) == 1 && await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == StepThreeNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return entry.GetVar(0) == 2 && await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return entry.GetVar(0) == 3 && await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await DefaultCloseDialogAsync(env, conn, 3, 3, reward: true, sameNpc: true, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
