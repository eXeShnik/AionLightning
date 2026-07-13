// Port of Java data/scripts/system/handlers/quest/katalam/_22522ScoutingOutentryPost.java (Romanz).
// Start at 800991; onAtDistance near 206319 advances var 0->1; kill 231202-231205 (x4) then flip to
// reward; turn in at 800991. Unblocked by the new onAtDistance hook.
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

public sealed class _22522ScoutingOutentryPost : QuestHandlerBase
{
    private const int QuestIdConst = 22522;
    private const int Npc          = 800991;
    private static readonly int[] MobIds = { 231202, 231203, 231204, 231205 };

    public _22522ScoutingOutentryPost(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Npc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc).OnTalk.Add(QuestId);
        RegisterOnAtDistance(engine, 206319);
        foreach (int mob in MobIds)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == Npc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                    return await SendQuestStartDialogAsync(env, conn, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == Npc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnAtDistanceAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        // Java changeQuestStep(env, 0, 1, false) self-guards on var 0 == 0.
        if (entry.GetVar(0) == 0)
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => await DefaultOnKillEventAsync(env, conn, MobIds, 1, 5, ct)
        || await DefaultOnKillEventAsync(env, conn, MobIds, 5, true, ct);
}
