// Port of Java data/scripts/system/handlers/quest/verteron/_1013HuntingLepharistRevolutionaries.java (MrPoke).
// Talk to Baaruk (203126), kill 210688 x10 (var 1->11), talk again (movie 25), kill 210316 once
// (var 12 -> REWARD). Zone-mission-end/level-up gated on quest 1130.
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Verteron;

public sealed class _1013HuntingLepharistRevolutionaries : QuestHandlerBase
{
    private const int QuestIdConst = 1013;
    private const int BaarukNpc    = 203126;
    private const int MobNpc       = 210688;
    private const int BossNpc      = 210316;

    public _1013HuntingLepharistRevolutionaries(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(BaarukNpc).OnTalk.Add(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(MobNpc).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(BossNpc).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1130, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || env.TargetId != BaarukNpc) return false;

        int var = entry.GetVar(0);
        int targetObjId = env.Target?.ObjectId ?? 0;

        switch (DialogActionLookup.FromId(env.DialogId))
        {
            case DialogAction.QUEST_SELECT when var == 0:
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            case DialogAction.QUEST_SELECT when var == 11:
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            case DialogAction.QUEST_SELECT when var >= 12:
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            case DialogAction.SELECT_ACTION_1012:
                await PlayQuestMovieAsync(conn, env.Player, 25, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
            case DialogAction.SETPRO1 or DialogAction.SETPRO2 when var is 0 or 11:
                entry.SetVar(0, var + 1);
                await UpdateQuestStatusAsync(conn, entry, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                return true;
            default:
                return false;
        }
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;

        if (targetId == MobNpc && var is >= 1 and <= 10)
        {
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
            return true;
        }
        if (targetId == BossNpc && var == 12)
        {
            await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
            return true;
        }
        return false;
    }
}
