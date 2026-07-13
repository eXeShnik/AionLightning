// Port of Java data/scripts/system/handlers/quest/beluslan/_28602IntotheUnknown.java (Gigi).
// Accept at 205234; talking 205234 (SETPRO1) opens the instance 300230000 (via EnterInstanceAsync)
// and sets var0 0->1; movie 454 end advances var0 1->2; use the object 700939 (var0 2->3); killing
// the boss 217005 (or the fallback spawn 217006) flips to REWARD; report to 205234. Leaving the
// instance world or dying while var0>0 resets var0 to 0 (must restart the run).
// Skips vs Java (cosmetic, state kept): onAtDistance applies protective effect 19288 via SkillEngine -
// no skill-effect apply API in this port, so the buff is dropped (environmental aura, not progression).
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

namespace Quest.Beluslan;

public sealed class _28602IntotheUnknown : QuestHandlerBase
{
    private const int QuestIdConst  = 28602;
    private const int StartNpc      = 205234;
    private const int UseObjectNpc  = 700939;
    private const int BossNpc       = 217005;
    private const int FallbackNpc   = 217006;
    private const int InstanceWorld = 300230000;
    private const int MovieId       = 454;

    public _28602IntotheUnknown(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService) { }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnEnterWorld(QuestId);
        RegisterOnDie(engine);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(UseObjectNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(BossNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(FallbackNpc).OnKill.Add(QuestId);
        RegisterOnAtDistance(engine, UseObjectNpc);
        engine.RegisterOnQuestMovieEnd(MovieId, QuestId);
    }

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (player.Position.WorldId != InstanceWorld && entry.GetVar(0) > 0)
        {
            entry.SetVar(0, 0);
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDieAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (entry.GetVar(0) > 0)
        {
            entry.SetVar(0, 0);
            await UpdateQuestStatusAsync(conn, entry, ct);
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
        if (env.TargetId == FallbackNpc)
        {
            entry.Status = QuestStatus.REWARD;
            await UpdateQuestStatusAsync(conn, entry, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnMovieEndAsync(QuestEnv env, int movieId, GsClientConnection conn, CancellationToken ct)
    {
        if (movieId != MovieId) return false;
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        if (entry.GetVar(0) == 1) // changeQuestStep(env, 1, 2, false): guard var0 == 1
        {
            entry.SetVar(0, 2);
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        // note: Java also opens the start branch on qs.canRepeat() - repeat-cooldown not ported, treated as "no active entry".
        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await EnterInstanceAsync(player, conn, InstanceWorld, 244.98566f, 244.14162f, 189.52058f, 30, ct);
                    if (entry.GetVar(0) == 0) // changeQuestStep(env, 0, 1, false): guard var0 == 0
                    {
                        entry.SetVar(0, 1);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                    }
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                }
            }
            else if (targetId == UseObjectNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                {
                    if (var == 2)
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                }
                else if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override ValueTask<bool> OnAtDistanceAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        // note: Java applies protective effect 19288 (SkillEngine.applyEffectDirectly) when in START/COMPLETE
        // and not already buffed - no skill-effect apply API in this port, so this is a documented no-op.
        return ValueTask.FromResult(false);
    }
}
