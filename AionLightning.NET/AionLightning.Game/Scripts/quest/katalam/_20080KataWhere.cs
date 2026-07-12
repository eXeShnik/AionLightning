// Port of Java data/scripts/system/handlers/quest/katalam/_20080KataWhere.java (Cheatkiller).
// Asmodian counterpart of 10080 ToKatalam. Auto-starts on level-up, relay chain 800170 (var0->1)
// -> 205625 (var1->2, teleports to 600050000) -> OnEnterWorld auto-advances var2->3 with movie 822
// once the player is in that world -> 800528 SET_SUCCEED flips straight to REWARD; turn in at
// 800529.
// Skip vs Java: 205625's TeleportService2.teleportTo call is omitted (no TeleportService2 in this
// port) — the var/status transition still happens so the quest completes.
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

public sealed class _20080KataWhere : QuestHandlerBase
{
    private const int QuestIdConst   = 20080;
    private const int FirstNpc       = 800170;
    private const int SecondNpc      = 205625;
    private const int ThirdNpc       = 800528;
    private const int TurnInNpc      = 800529;
    private const int KatalamWorldId = 600050000;

    public _20080KataWhere(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        engine.RegisterQuestNpc(FirstNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ThirdNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (player.Position.WorldId != KatalamWorldId) return false;
        if (entry.GetVar(0) != 2) return false;

        await PlayQuestMovieAsync(conn, player, 822, ct);
        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START)
        {
            if (entry is not null && entry.Status == QuestStatus.REWARD && env.TargetId == TurnInNpc)
            {
                if (DialogActionLookup.FromId(env.DialogId) == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, env.Target?.ObjectId ?? 0, 10002, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (targetId == FirstNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }
        if (targetId == SecondNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 1)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            return false;
        }
        if (targetId == ThirdNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 3)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            if (dialog == DialogAction.SET_SUCCEED)
                return await DefaultCloseDialogAsync(env, conn, 3, 3, reward: true, sameNpc: false, ct);
            return false;
        }

        return false;
    }
}
