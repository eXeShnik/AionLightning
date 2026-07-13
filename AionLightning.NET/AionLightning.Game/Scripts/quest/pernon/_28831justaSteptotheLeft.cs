// Port of Java data/scripts/system/handlers/quest/pernon/_28831justaSteptotheLeft.java (zhkchi).
// Accept at 830651; talk to any of 5 butler-template npcs (810022-810026) while START (USE_OBJECT ->
// dialog 2375; SELECT_QUEST_REWARD flips var 0->0 to REWARD, dialog 5); turn in at the same butler
// while REWARD (any dialog there is handed straight to the end-dialog helper, matching Java's direct
// `return sendQuestEndDialog(env);` with no inner dialog-action switch).
// Skip vs Java (documented): the Java handler also requires `player.getActiveHouse() != null &&
// house.getButler().getNpcId() == targetId` (your OWN house's butler only) while START. This port has
// no House/ActiveHouse/Butler model at all (see migration_plan.md housing gap), so that ownership
// gate is omitted; any of the 5 butler npc templates is accepted. This only widens who may interact
// with the butler dialog — no var/status transition changes.
using System.Collections.Generic;
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

namespace Quest.Pernon;

public sealed class _28831justaSteptotheLeft : QuestHandlerBase
{
    private const int QuestIdConst = 28831;
    private const int StartNpc     = 830651;

    private static readonly HashSet<int> Butlers = [810022, 810023, 810024, 810025, 810026];

    public _28831justaSteptotheLeft(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        foreach (int butlerId in Butlers)
            engine.RegisterQuestNpc(butlerId).OnTalk.Add(QuestId);
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
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog is DialogAction.QUEST_ACCEPT_1 or DialogAction.QUEST_ACCEPT_SIMPLE)
                    return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && Butlers.Contains(targetId))
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && Butlers.Contains(targetId))
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
