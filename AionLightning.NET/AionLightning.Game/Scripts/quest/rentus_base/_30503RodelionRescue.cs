// Port of Java data/scripts/system/handlers/quest/rentus_base/_30503RodelionRescue.java
// (maddison). Start at Lition (205438); free Rodelion (799541, SET_SUCCEED -> straight to
// REWARD); turn in back at Lition.
//
// Java bug fixed: the START branch tested `targetId == 799541` twice — once as `if`, once as an
// unreachable `else if` on the exact same condition (dead code, since the first branch already
// consumes every match). Register() also listed 701097 for OnTalk, which is otherwise never
// handled anywhere in the file — the dead branch's `Npc npc = ...; npc.getController().onDelete();
// return true;` was clearly meant for that npc (a companion the player frees but doesn't hand a
// dialog step to), not a second copy of 799541. Rewired to target 701097.
// Skip vs Java: the freed companion's getController().onDelete() (despawn) has no equivalent (no
// NPC controller/despawn subsystem in this port yet) — the interaction is still acknowledged
// (returns true) so the client dialog closes normally.
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

public sealed class _30503RodelionRescue : QuestHandlerBase
{
    private const int QuestIdConst = 30503;
    private const int StartNpc     = 205438;
    private const int RodelionNpc  = 799541;
    private const int CompanionNpc = 701097;

    public _30503RodelionRescue(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RodelionNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CompanionNpc).OnTalk.Add(QuestId);
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
            if (targetId == RodelionNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                {
                    await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == CompanionNpc)
                return true; // Java: npc.getController().onDelete() — dropped, see header.

            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
