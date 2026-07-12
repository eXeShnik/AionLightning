// Port of Java data/scripts/system/handlers/quest/greater_stigma/_21552ToBeaSiegeMaster.java
// (zhkchi). Asmodian mirror of _11552TheSiegeisOn: accept at 205613, attack 4 siege npcs in
// sequence (259014 -> 259214 -> 259414 -> 259614, var 0->1->2->3->4 toReward on the last one); turn
// in at 205613. See _11552TheSiegeisOn for the dead-code onKillInWorldEvent note, the canRepeat
// simplification, and the QUEST_ACCEPT_SIMPLE fix (all ported identically here).
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

namespace Quest.GreaterStigma;

public sealed class _21552ToBeaSiegeMaster : QuestHandlerBase
{
    private const int QuestIdConst = 21552;
    private const int StartNpc     = 205613;
    private const int SiegeNpc1    = 259014;
    private const int SiegeNpc2    = 259214;
    private const int SiegeNpc3    = 259414;
    private const int SiegeNpc4    = 259614;

    public _21552ToBeaSiegeMaster(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SiegeNpc1).OnAttack.Add(QuestId);
        engine.RegisterQuestNpc(SiegeNpc2).OnAttack.Add(QuestId);
        engine.RegisterQuestNpc(SiegeNpc3).OnAttack.Add(QuestId);
        engine.RegisterQuestNpc(SiegeNpc4).OnAttack.Add(QuestId);
    }

    // Java onKillInWorldEvent (dead code — never registered via registerOnKillInWorld in the Java
    // source, so the engine never dispatches it) — ported for fidelity, never dispatched here either.
    public override async ValueTask<bool> OnPlayerKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var < 9)
        {
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
            return true;
        }
        if (var == 9)
        {
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnAttackAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;

        if (var == 0 && targetId == SiegeNpc1) { await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct); return true; }
        if (var == 1 && targetId == SiegeNpc2) { await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct); return true; }
        if (var == 2 && targetId == SiegeNpc3) { await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct); return true; }
        if (var == 3 && targetId == SiegeNpc4) { await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: true, ct); return true; }

        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId != StartNpc) return false;

        if (entry is null)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
