// Port of Java data/scripts/system/handlers/quest/heiron/_1063BrigadierIndratu.java.
// Talk to Perento (204500, var0->1), kill 214159 (var1->2, plays movie 424 on every kill while
// active, matching Java), then Perento's SET_SUCCEED flips straight to REWARD; turn in at
// Fasimede (203700). No preceding-quest gate (starts via its own zone-mission-end/level-up hook).
// Skip vs Java: the teleport-to-210040000 on SETPRO1 isn't ported — no teleport service exists in
// this port yet; the var transition and dialog close still apply.
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

namespace Quest.Heiron;

public sealed class _1063BrigadierIndratu : QuestHandlerBase
{
    private const int QuestIdConst = 1063;
    private const int PerentoNpc  = 204500;
    private const int FasimedeNpc = 203700;
    private const int KillNpc     = 214159;

    public _1063BrigadierIndratu(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(KillNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(PerentoNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FasimedeNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId != PerentoNpc) return false;
            int var = entry.GetVar(0);
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO1)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            if (dialog == DialogAction.SET_SUCCEED)
                return await DefaultCloseDialogAsync(env, conn, 2, 2, reward: true, sameNpc: false, ct);
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == FasimedeNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        await DefaultOnKillEventAsync(env, conn, KillNpc, 1, 2, ct);
        await PlayQuestMovieAsync(conn, env.Player, 424, ct);
        return true;
    }
}
