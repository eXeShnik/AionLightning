// Port of Java data/scripts/system/handlers/quest/rider_quests/_14012DukakiMischief.java (pralinka).
// Zone-mission sub-quest of 14010: talk to the quest npc (203129, var0 0->1), kill 5 Dukaki
// (210145/210146, var index 1) and 3 Tursin (210157, var index 2), report back, turn in at 203098.
// Java bug: onDialogEvent's switch on 203129 had no breaks after the QUEST_SELECT/SETPRO1 case
// bodies, so talking with var0==1 (regardless of the var1==5/var2==3 sub-objective progress) fell
// through into the SETPRO3 body and flipped the quest straight to REWARD. Fixed here so the REWARD
// flip only fires on an actual SETPRO3 dialog.
using System.Linq;
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

namespace Quest.RiderQuests;

public sealed class _14012DukakiMischief : QuestHandlerBase
{
    private const int QuestIdConst = 14012;
    private const int QuestNpc     = 203129;
    private const int GateGuardNpc = 203098;
    private static readonly int[] _dukaki = [210145, 210146];
    private static readonly int[] _tursin = [210157];

    public _14012DukakiMischief(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(QuestNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GateGuardNpc).OnTalk.Add(QuestId);
        foreach (int mob in _dukaki) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        foreach (int mob in _tursin) engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 14010, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            if (env.TargetId != QuestNpc) return false;

            int var0 = entry.GetVar(0), var1 = entry.GetVar(1), var2 = entry.GetVar(2);

            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var0 == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var0 == 1 && var1 == 5 && var2 == 3) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                return false;
            }
            if (dialog == DialogAction.SETPRO1)
                return var0 == 0 && await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.SETPRO3)
            {
                if (var0 != 1) return false;
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
            return env.TargetId == GateGuardNpc && await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;

        int targetId = env.TargetId;
        if (_dukaki.Contains(targetId))
            return await BumpVarAsync(conn, entry, varNum: 1, startVar: 0, endVar: 5, ct);
        if (_tursin.Contains(targetId))
            return await BumpVarAsync(conn, entry, varNum: 2, startVar: 0, endVar: 3, ct);
        return false;
    }

    private async ValueTask<bool> BumpVarAsync(GsClientConnection conn, QuestEntry entry, int varNum, int startVar, int endVar, CancellationToken ct)
    {
        int var = entry.GetVar(varNum);
        if (var < startVar || var >= endVar) return false;
        await ChangeQuestStepAsync(conn, entry, varNum, var + 1, toReward: false, ct);
        return true;
    }
}
