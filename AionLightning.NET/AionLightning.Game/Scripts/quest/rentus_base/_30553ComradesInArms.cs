// Port of Java data/scripts/system/handlers/quest/rentus_base/_30553ComradesInArms.java (Ritsu).
// Start at Lition (205438); a companion npc (701097) is freed on talk (no dialog gate in Java);
// report to Rodelion (799541, SET_SUCCEED -> var0->1, reward); turn in at Lition itself
// (sameNpc — SELECT_QUEST_REWARD there flips straight to reward and shows the end dialog).
//
// Java bug fixed: the outer `switch (targetId) { case 205438: {...} case 701097: {...}
// case 799541: {...} }` has no break after the 205438 block, so any dialog sent to 205438 other
// than SELECT_QUEST_REWARD (e.g. QUEST_SELECT) falls through unconditionally into 701097's
// onDelete()+return true — despawning the wrong npc. Fixed by checking each npc branch
// independently, matching the convention used elsewhere in this batch (see
// ascension._1913DispatchtoVerteron).
// Skip vs Java: the companion's getController().onDelete() (despawn) has no equivalent (no NPC
// controller/despawn subsystem in this port yet) — the interaction is still acknowledged.
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

namespace Quest.RentusBase;

public sealed class _30553ComradesInArms : QuestHandlerBase
{
    private const int QuestIdConst = 30553;
    private const int StartNpc     = 205438; // Lition
    private const int CompanionNpc = 701097;
    private const int RodelionNpc  = 799541;

    public _30553ComradesInArms(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CompanionNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RodelionNpc).OnTalk.Add(QuestId);
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
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await DefaultCloseDialogAsync(env, conn, 1, 1, reward: true, sameNpc: true, ct);
                return false;
            }

            if (targetId == CompanionNpc)
                return true; // Java: npc.getController().onDelete() — dropped, see header.

            if (targetId == RodelionNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: true, sameNpc: false, ct);
                return false;
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
