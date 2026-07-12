// Port of Java data/scripts/system/handlers/quest/katalam/_13030CombatMechanic.java (Evil_dnk).
// Talk to 801097 to start; interacting with any of 3 training dummies (701685/701686/701687)
// advances var 0->1->2, then straight to REWARD at var 2; turn in at 801097.
// Java's npc.getController().die() (kills the dummy) is accepted but ignored — no NPC
// controller/death-trigger infra wired to quest scripts (same simplification as UseQuestObjectAsync's
// dieObject flag elsewhere).
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

namespace Quest.Katalam;

public sealed class _13030CombatMechanic : QuestHandlerBase
{
    private const int QuestIdConst = 13030;
    private const int StartNpc     = 801097;
    private const int Dummy1       = 701685;
    private const int Dummy2       = 701686;
    private const int Dummy3       = 701687;

    public _13030CombatMechanic(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Dummy1).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Dummy2).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Dummy3).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && (targetId == Dummy1 || targetId == Dummy2 || targetId == Dummy3))
        {
            int var = entry.GetVar(0);
            if (var < 2)
            {
                await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            if (var == 2)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: true, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
