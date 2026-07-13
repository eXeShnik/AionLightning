// Port of Java data/scripts/system/handlers/quest/rider_quests/_24024ANepraProtector.java (pralinka).
// Zone-mission sub-quest of 24020: talk to 204369 (movie 80, var0 0->1), talk to 204361 (var0 1->2),
// enter the Altar of the Black Dragon zone (movie 81, var0 2->3), kill mob 212861 (var0 3->reward),
// turn in at 204369.
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

public sealed class _24024ANepraProtector : QuestHandlerBase
{
    private const int QuestIdConst = 24024;
    private const int FirstNpc  = 204369;
    private const int SecondNpc = 204361;
    private const int GuardianMob = 212861;
    private const string AltarZone = "ALTAR_OF_THE_BLACK_DRAGON_220020000";

    public _24024ANepraProtector(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(GuardianMob).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(FirstNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, AltarZone);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 24020, isZoneMission: true, ct);

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        if (zoneName != AltarZone) return false;
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;

        await PlayQuestMovieAsync(conn, player, 81, ct);
        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
        return true;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, GuardianMob, startVar: 3, reward: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == FirstNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    if (var != 0) return false;
                    await PlayQuestMovieAsync(conn, player, 80, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                return false;
            }
            if (targetId == SecondNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return var == 1 && await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == FirstNpc)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
