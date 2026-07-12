// Port of Java data/scripts/system/handlers/quest/theobomos/_3057CursedZirius.java.
// Talk to Erisilith (798213) to start; attacking the cursed Zirius (214576) finishes it off and
// flips straight to REWARD (Java onAttackEvent + MathUtil distance gate); turn in at Erisilith.
// Uses the Batch 0.3 OnAttack hook. Skip vs Java: the distance check against a fixed point
// (1691.41, 219.09, 72.62 <= 30) and the explicit Npc.getController().onDie() kill are omitted
// (no distance/position API or NPC controller wired into OnAttackAsync yet) — any attack on the
// registered mob while the quest is at START advances it, which is a superset of the Java gate
// but keeps the quest completable.
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

namespace Quest.Theobomos;

public sealed class _3057CursedZirius : QuestHandlerBase
{
    private const int QuestIdConst = 3057;
    private const int ErisilithNpc = 798213;
    private const int ZiriusNpc    = 214576;

    public _3057CursedZirius(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(ErisilithNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(ErisilithNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ZiriusNpc).OnAttack.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != ErisilithNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ErisilithNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnAttackAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != ZiriusNpc) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
        return true;
    }
}
