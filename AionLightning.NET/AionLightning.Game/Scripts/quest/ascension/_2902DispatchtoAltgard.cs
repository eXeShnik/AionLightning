// Port of Java data/scripts/system/handlers/quest/ascension/_2902DispatchtoAltgard.java.
// Auto-starts on level-up (no preconditions); talk to the Dispatch Officer (204191, var0->1) then
// to the Altgard contact (203559, var1->REWARD) to finish.
//
// Skip vs Java: the officer's SETPRO1 branch calls
// TeleportService2.teleportTo(player, 220030000, player.getInstanceId(), 1748f, 1807f, 255f) to
// move the player to Altgard - no TeleportService2 exists in this port. The var advance +
// close-dialog is kept so the quest stays completable at the contact npc without the teleport.
//
// Java bug fixed: the outer `switch (targetId) { case 204191: {...} case 203559: ... }` has no
// break after the 204191 block, so a QUEST_SELECT click on the officer after var has already
// advanced past 0 falls through into the contact npc's REWARD-flip branch (using the same dialog
// action, ignoring that the actual target was the officer). This port checks each npc branch
// independently instead of relying on that fallthrough, matching the convention used elsewhere in
// this batch (see _1007ACeremonyinSanctum).
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

namespace Quest.Ascension;

public sealed class _2902DispatchtoAltgard : QuestHandlerBase
{
    private const int QuestIdConst = 2902;
    private const int OfficerNpc   = 204191;
    private const int ContactNpc   = 203559;

    public _2902DispatchtoAltgard(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(OfficerNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ContactNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == OfficerNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1 && var == 0)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    // Skip: Java teleports to 220030000 here (TeleportService2 not ported) - see header.
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == ContactNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                }
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ContactNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
