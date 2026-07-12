// Port of Java data/scripts/system/handlers/quest/oriel/_18830MovingIn.java (Rolandas).
// Accept at 830584 (start-only npc, no talk registration in Java either); talk to any of 5
// butler-template npcs (810017-810021) while var 0 == 0 (SETPRO1 advances 0->1, dialog 1352); then
// at 830645, USE_OBJECT while still START flips var slot 1 to 1 + REWARD (dialog 2375); turn in at
// 830645.
// Skip vs Java (documented), both around the House model gap (no HousingService/House/Butler exists
// in this port at all — see migration_plan.md housing gap):
//  1) The top-of-handler `player.getActiveHouse() == null` gate and the per-branch
//     `house.getButler().getNpcId() != targetId` ownership check (your OWN house's butler only) are
//     both omitted — any of the 5 butler npc templates is accepted. Widens who may interact with the
//     butler dialog only; no var/status transition changes.
//  2) `qe.registerQuestHouseItem(questId)` + the `onHouseItemUseEvent` override (an alternate trigger
//     that flips var slot 1 to 1 + REWARD when var 0 == 1, exactly like the 830645/USE_OBJECT branch
//     below) has no equivalent hook in this port (no house-item-use event is wired to the quest
//     engine at all). This is a redundant alternate path — the direct dialog turn-in at 830645 already
//     reaches the same var/REWARD transition, so the quest still completes end-to-end without it.
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

public sealed class _18830MovingIn : QuestHandlerBase
{
    private const int QuestIdConst = 18830;
    private const int StartNpc     = 830584;
    private const int TurnInNpc    = 830645;

    private static readonly HashSet<int> Butlers = [810017, 810018, 810019, 810020, 810021];

    public _18830MovingIn(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        foreach (int butlerId in Butlers)
            engine.RegisterQuestNpc(butlerId).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
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

        if (entry.Status == QuestStatus.START && Butlers.Contains(targetId) && entry.GetVar(0) == 0)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            if (dialog == DialogAction.SELECTED_QUEST_NOREWARD)
            {
                await SendQuestEndDialogAsync(env, conn, ct);
                return true;
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
            {
                await ChangeQuestStepAsync(conn, entry, 1, 1, toReward: true, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            }
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
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
