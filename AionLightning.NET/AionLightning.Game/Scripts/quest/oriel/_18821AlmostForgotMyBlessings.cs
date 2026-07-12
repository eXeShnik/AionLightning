// Port of Java data/scripts/system/handlers/quest/oriel/_18821AlmostForgotMyBlessings.java (Rolandas).
// Talk to any of 6 butler-template npcs (810017-810022) to start (repeatable — canRepeat() is
// approximated as "no active entry", the same simplification used across ~7 other zones since this
// port has no repeatable-quest/nextRepeatTime modeling yet); single dialog step at the same butler
// (var 0->0, reward, dialog 5) to turn in.
// Skip vs Java (documented): the Java handler additionally requires `player.getActiveHouse() != null
// && house.getButler().getNpcId() == targetId` before accepting any dialog — i.e. you may only use
// YOUR OWN house's butler. This port has no House/ActiveHouse/Butler model at all (see
// migration_plan.md housing gap), so that ownership gate is omitted; any of the 6 butler npc
// templates is accepted. This only widens who may interact with the butler dialog — it does not
// change any var/status transition, so the quest still starts/advances/completes identically.
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

namespace Quest.Oriel;

public sealed class _18821AlmostForgotMyBlessings : QuestHandlerBase
{
    private const int QuestIdConst = 18821;

    private static readonly HashSet<int> Butlers = [810017, 810018, 810019, 810020, 810021, 810022];

    public _18821AlmostForgotMyBlessings(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        foreach (int butlerId in Butlers)
        {
            engine.RegisterQuestNpc(butlerId).OnQuestStart.Add(QuestId);
            engine.RegisterQuestNpc(butlerId).OnTalk.Add(QuestId);
        }
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        if (!Butlers.Contains(targetId)) return false;

        var entry = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog is DialogAction.QUEST_ACCEPT_1 or DialogAction.QUEST_ACCEPT_SIMPLE)
                return await SendQuestStartDialogAsync(env, conn, ct);
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            if (dialog == DialogAction.SELECTED_QUEST_NOREWARD)
                return await SendQuestEndDialogAsync(env, conn, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            if (dialog == DialogAction.SELECTED_QUEST_NOREWARD)
            {
                await SendQuestEndDialogAsync(env, conn, ct);
                return true;
            }
            return false;
        }

        return false;
    }
}
