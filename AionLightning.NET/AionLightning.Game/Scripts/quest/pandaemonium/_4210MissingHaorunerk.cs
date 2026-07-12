// Port of Java data/scripts/system/handlers/quest/pandaemonium/_4210MissingHaorunerk.java.
// Accept default at 204283; talk-step at 798332 (var0 0->1); two independent kill flags tracked in
// var slots 1/2 (mobs 215056/215080, each 0->1 on first kill — bespoke since it doesn't fit the
// flat span-counter helper); once both are set, USE_OBJECT at 798331 shows the check dialog and
// SELECT_QUEST_REWARD there completes it in place (sameNpc); turn in at 798331.
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

public sealed class _4210MissingHaorunerk : QuestHandlerBase
{
    private const int QuestIdConst = 4210;
    private const int StartNpc = 204283;
    private const int TurnInNpc = 798331;
    private const int TalkNpc = 798332;
    private const int MobA = 215056;
    private const int MobB = 215080;

    public _4210MissingHaorunerk(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(TalkNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobA).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(MobB).OnKill.Add(QuestId);
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
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == TalkNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return entry.GetVar(0) == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return entry.GetVar(1) == 1 && entry.GetVar(2) == 1 && await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: true, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != TurnInNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (env.TargetId == MobA && entry.GetVar(1) == 0)
        {
            await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: false, ct);
            return true;
        }
        if (env.TargetId == MobB && entry.GetVar(2) == 0)
        {
            await ChangeQuestStepAsync(conn, entry, 2, 1, toReward: false, ct);
            return true;
        }
        return false;
    }
}
