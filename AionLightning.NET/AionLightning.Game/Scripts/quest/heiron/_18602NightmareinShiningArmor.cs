// Port of Java data/scripts/system/handlers/quest/heiron/_18602NightmareinShiningArmor.java.
// Talk to Kyrie (205229) to start; SETPRO1 creates a fresh instance of 300230000 and teleports the
// player in (var0->1). Movie 454 advances var0 1->2; use object 700939 (SETPRO3) advances 2->3;
// killing boss 217005 (or fallback 217006) flips to REWARD. Leaving the instance world or dying
// while in progress resets var0 to 0. Turn in at 205229.
// Skip vs Java: qs.canRepeat() (daily-repeat re-offer) isn't ported in this port, so only a
// first-time run is offered. onAtDistance of 700939 applies a cosmetic transformation buff (skill
// 19288) which is dropped (no direct skill-apply infra) — see note in OnAtDistanceAsync.
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

public sealed class _18602NightmareinShiningArmor : QuestHandlerBase
{
    private const int QuestIdConst = 18602;
    private const int StartNpc     = 205229;
    private const int UseObjectNpc = 700939;
    private const int BossNpc      = 217005;
    private const int FallbackBoss = 217006;
    private const int InstanceWorld = 300230000;
    private const int MovieId      = 454;

    public _18602NightmareinShiningArmor(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnEnterWorld(QuestId);
        RegisterOnDie(engine);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(UseObjectNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(BossNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(FallbackBoss).OnKill.Add(QuestId);
        RegisterOnAtDistance(engine, UseObjectNpc);
        engine.RegisterOnQuestMovieEnd(MovieId, QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.Player.Position.WorldId == InstanceWorld) return false;

        int var = entry.GetVar(0);
        if (var > 0)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var > 0)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (env.TargetId == BossNpc)
            return await DefaultOnKillEventAsync(env, conn, BossNpc, 3, reward: true, ct);
        if (env.TargetId == FallbackBoss)
        {
            await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != MovieId) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        // Java: qs == null || NONE || canRepeat() — canRepeat() (daily re-offer) not ported.
        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (env.TargetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (env.TargetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await EnterInstanceAsync(player, conn, InstanceWorld, 244.98566f, 244.14162f, 189.52058f, (byte)30, ct);
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
            }
            else if (env.TargetId == UseObjectNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                {
                    if (var == 2)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                }
                else if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (env.TargetId == StartNpc)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override ValueTask<bool> OnAtDistanceAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        // note: Java applies a cosmetic transformation buff (SkillEngine.applyEffectDirectly(19288))
        // when in range of 700939 while START/COMPLETE; no direct skill-apply infra, so dropped.
        return ValueTask.FromResult(false);
    }
}
