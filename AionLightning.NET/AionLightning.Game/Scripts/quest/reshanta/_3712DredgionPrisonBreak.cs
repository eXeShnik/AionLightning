// Port of Java data/scripts/system/handlers/quest/reshanta/_3712DredgionPrisonBreak.java
// (Cheatkiller). Start at 279045; freeing either prisoner npc (798323 or 798326) at var0==0 flips
// straight to REWARD; turn in back at 279045.
// Dropped a latent dead branch: Java also had an onKillEvent override for npc 214823, but (a) that
// npc was never registered via addOnKillEvent in this file's own register() (so the engine would
// never dispatch a kill to it) and (b) even if it were, the dialog step above already flips the
// quest to REWARD before any kill could matter, so the status==START guard inside
// defaultOnKillEvent would always fail — omitted here as genuinely unreachable in both versions.
// Skip vs Java: the freed prisoner's getController().onDelete() (despawn-on-free) has no equivalent
// (no NPC controller/despawn subsystem in this port yet) — the var transition still applies.
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

namespace Quest.Reshanta;

public sealed class _3712DredgionPrisonBreak : QuestHandlerBase
{
    private const int QuestIdConst = 3712;
    private const int StartNpc     = 279045;
    private const int PrisonerNpc1 = 798323;
    private const int PrisonerNpc2 = 798326;

    public _3712DredgionPrisonBreak(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PrisonerNpc1).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PrisonerNpc2).OnTalk.Add(QuestId);
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && (targetId == PrisonerNpc1 || targetId == PrisonerNpc2))
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.GetVar(0) == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: true, sameNpc: false, ct);
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
