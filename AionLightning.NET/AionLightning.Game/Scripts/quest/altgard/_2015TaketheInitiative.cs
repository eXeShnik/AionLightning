// Port of Java data/scripts/system/handlers/quest/altgard/_2015TaketheInitiative.java (MrPoke).
// Accept from NPC 203631, kill three separate mob groups tracked in vars 1/2/3 (1 kill, 5 kills,
// 5 kills), then report back once all three thresholds are met. Zone-mission chain: preceded by
// 2014, also level-up gated.
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

namespace Quest.Altgard;

public sealed class _2015TaketheInitiative : QuestHandlerBase
{
    private const int QuestIdConst = 2015;
    private const int NokirNpc     = 203631;
    private const int Mob1         = 210510;
    private const int Mob2         = 210504;
    private const int Mob3         = 210506;

    public _2015TaketheInitiative(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(NokirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(Mob1).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Mob2).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(Mob3).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, precedingQuestId: 2014, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2200, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || env.TargetId != NokirNpc) return false;
        int var = entry.GetVar(0);
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START)
        {
            if (dialog == DialogAction.USE_OBJECT
                && entry.GetVar(1) >= 1 && entry.GetVar(2) >= 5 && entry.GetVar(3) >= 5)
            {
                entry.SetVar(0, var + 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            }
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }
        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        switch (env.TargetId)
        {
            case Mob1:
                if (entry.GetVar(1) == 0)
                {
                    entry.SetVar(1, 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                }
                break;
            case Mob2:
                if (entry.GetVar(2) < 5)
                {
                    entry.SetVar(2, entry.GetVar(2) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                }
                break;
            case Mob3:
                if (entry.GetVar(3) < 5)
                {
                    entry.SetVar(3, entry.GetVar(3) + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                }
                break;
        }
        return false; // Java parity: this handler never reports the kill as "consumed"
    }
}
