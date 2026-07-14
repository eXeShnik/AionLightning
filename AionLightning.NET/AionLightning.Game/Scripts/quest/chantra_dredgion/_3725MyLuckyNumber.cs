// Port of Java data/scripts/system/handlers/quest/chantra_dredgion/_3725MyLuckyNumber.java.
// Start at Yannis (799069); turn-in at Yulia (798928). Progress is a dredgion-run counter on var1
// (0->6, one per dredgion completion) plus a kill counter on var2 (0->15, mobs 281866/216866).
// Quest opens the turn-in dialog only once var1==6 && var2==15.
// note: dredgion scoring subsystem isn't ported yet, so QuestEngine.OnDredgionRewardAsync never
// fires — OnDredgionRewardAsync below compiles and registers but is currently unreachable; var1 can
// therefore not advance until dredgion scoring lands. Ported faithfully regardless.
// note: Java's repeat guard qs.canRepeat() (daily-reset re-entry) is dropped — no QuestEntry.CanRepeat
// exists in this codebase yet (same omission as every other repeatable quest already ported, e.g.
// chantra_dredgion's _3722MyNewToy).
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

public sealed class _3725MyLuckyNumber : QuestHandlerBase
{
    private const int QuestIdConst = 3725;
    private const int YannisNpc    = 799069;
    private const int YuliaNpc     = 798928;
    private const int KillNpc1     = 281866;
    private const int KillNpc2     = 216866;

    public _3725MyLuckyNumber(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        RegisterOnDredgionReward(engine);
        engine.RegisterQuestNpc(YannisNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(YannisNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(YuliaNpc).OnTalk.Add(QuestId);
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
            if (targetId == YannisNpc)
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
            if (targetId == YuliaNpc)
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
            if (targetId == YuliaNpc)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        // Java: defaultOnKillEvent(env, {281866, 216866}, 0, 15, 2) — counts on quest var 2 (0..15).
        // No varNum-aware DefaultOnKillEventAsync overload exists (the base helper hardcodes var 0),
        // so the span logic is inlined here on var 2 to stay faithful.
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
