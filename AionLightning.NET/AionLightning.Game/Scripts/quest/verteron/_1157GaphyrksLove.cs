// Port of Java data/scripts/system/handlers/quest/verteron/_1157GaphyrksLove.java (Rhys2002).
// Accept from 798003, attack the target mob (210319) within 13m of a fixed spot to trigger the
// reveal movie, then turn in.
using System;
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

namespace Quest.Verteron;

public sealed class _1157GaphyrksLove : QuestHandlerBase
{
    private const int QuestIdConst = 1157;
    private const int StartNpc     = 798003;
    private const int MobNpc       = 210319;
    private const int RevealMovie  = 17;
    private const float SpotX = 892f;
    private const float SpotY = 2024f;
    private const float SpotZ = 166f;

    public _1157GaphyrksLove(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(MobNpc).OnAttack.Add(QuestId);
        engine.RegisterOnQuestMovieEnd(RevealMovie, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        if (env.TargetId != StartNpc) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnAttackAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != MobNpc || env.Target is null) return false;

        float dx = env.Target.Position.X - SpotX;
        float dy = env.Target.Position.Y - SpotY;
        float dz = env.Target.Position.Z - SpotZ;
        if (MathF.Sqrt(dx * dx + dy * dy + dz * dz) > 13f) return false;

        // Java also despawns/respawns the mob via its AI controller (scheduleRespawn/onDelete) —
        // no NPC controller infra exists yet to port this; harmless, the mob stays visible.
        await PlayQuestMovieAsync(conn, env.Player, RevealMovie, ct);
        return true;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != RevealMovie) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status == QuestStatus.COMPLETE) return false;

        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }
}
