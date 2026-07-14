// Port of Java data/scripts/system/handlers/quest/chantra_dredgion/_4725CeaselessAttack.java.
// Asmodian mirror of _3725MyLuckyNumber. Start at Yorgen (799403); turn-in at Valetta (799226).
// Progress is a dredgion-run counter on var1 (0->6, one per dredgion completion) plus a kill counter
// on var2 (0->15, mobs 281866/216866). Turn-in dialog opens only once var1==6 && var2==15.
// note: dredgion scoring subsystem isn't ported yet, so QuestEngine.OnDredgionRewardAsync never
// fires — OnDredgionRewardAsync below compiles and registers but is currently unreachable; var1 can
// therefore not advance until dredgion scoring lands. Ported faithfully regardless.
// note: Java's repeat guard qs.canRepeat() is dropped — no QuestEntry.CanRepeat exists yet (same
// omission as every other repeatable quest already ported here).
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

namespace Quest.ChantraDredgion;

public sealed class _4725CeaselessAttack : QuestHandlerBase
{
    private const int QuestIdConst = 4725;
    private const int YorgenNpc    = 799403;
    private const int ValettaNpc   = 799226;
    private const int KillNpc1     = 281866;
    private const int KillNpc2     = 216866;

    public _4725CeaselessAttack(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        RegisterOnDredgionReward(engine);
        engine.RegisterQuestNpc(YorgenNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(YorgenNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ValettaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KillNpc1).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(KillNpc2).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == YorgenNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }
        else if (entry.Status == QuestStatus.START)
        {
            int var1 = entry.GetVar(1);
            int var2 = entry.GetVar(2);
            if (targetId == ValettaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var1 == 6 && var2 == 15)
                        return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
                }
                else if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == ValettaNpc)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        // Java: defaultOnKillEvent(env, {281866, 216866}, 0, 15, 2) — counts on quest var 2 (0..15).
        // Inlined on var 2 because the base helper's span overload hardcodes var 0 (no varNum overload).
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != KillNpc1 && env.TargetId != KillNpc2) return false;

        int var2 = entry.GetVar(2);
        if (var2 < 0 || var2 >= 15) return false;

        await ChangeQuestStepAsync(conn, entry, 2, var2 + 1, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnDredgionRewardAsync(QuestEnv env, int rank, GsClientConnection conn, CancellationToken ct)
    {
        // note: unreachable until dredgion scoring is ported (see class header). Java ignores the
        // dredgion result rank here — any completion advances the var1 counter (0..6).
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var1 = entry.GetVar(1);
        if (var1 >= 6) return false;

        await ChangeQuestStepAsync(conn, entry, 1, var1 + 1, toReward: false, ct);
        return true;
    }
}
