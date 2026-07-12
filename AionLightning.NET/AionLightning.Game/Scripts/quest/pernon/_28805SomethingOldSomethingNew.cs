// Port of Java data/scripts/system/handlers/quest/pernon/_28805SomethingOldSomethingNew.java (Ritsu).
// Talk to 830154 to start; talk to any of 830521/830662/830663 (var 0->1 via SETPRO1, shown at
// dialog 1352), then use object 730525/730522 (var 1->2 via SETPRO2, shown at dialog 1693), then
// back to 830521/830662/830663 (var 2, dialog 2375) to flip to REWARD; turn in at the same npcs.
// Java bug fixed: the outer `switch (targetId)` case for {830521,830662,830663} had no `break`
// before falling into the `{730525,730522}` case — any dialog action on those three npcs that
// wasn't QUEST_SELECT/SETPRO1/SELECT_QUEST_REWARD (e.g. an unrelated client dialog id) fell
// through and re-ran the 730525/730522 USE_OBJECT/SETPRO2 logic against the wrong target. Ported
// as independent target-id branches so no such fallthrough happens. The remaining Java
// fallthroughs (QUEST_SELECT -> SETPRO1 when var is neither 0 nor 2; USE_OBJECT -> SETPRO2 when
// var != 1) are harmless — DefaultCloseDialogAsync re-validates the var and no-ops otherwise — so
// they're kept 1:1 rather than "fixed away".
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

public sealed class _28805SomethingOldSomethingNew : QuestHandlerBase
{
    private const int QuestIdConst = 28805;
    private const int StartNpc     = 830154;
    private const int KeeperNpc1   = 830521;
    private const int KeeperNpc2   = 830662;
    private const int KeeperNpc3   = 830663;
    private const int ChestNpc1    = 730525;
    private const int ChestNpc2    = 730522;

    public _28805SomethingOldSomethingNew(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KeeperNpc1).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KeeperNpc2).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KeeperNpc3).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ChestNpc1).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ChestNpc2).OnTalk.Add(QuestId);
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
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);

            if (targetId is KeeperNpc1 or KeeperNpc2 or KeeperNpc3)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                }
                if (dialog is DialogAction.QUEST_SELECT or DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    await ChangeQuestStepAsync(conn, entry, 2, 2, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return false;
            }

            if (targetId is ChestNpc1 or ChestNpc2)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog is DialogAction.USE_OBJECT or DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId is KeeperNpc1 or KeeperNpc2 or KeeperNpc3)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
