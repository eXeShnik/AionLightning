// Port of Java data/scripts/system/handlers/quest/verteron/_1012MaskedLoiterers.java (MrPoke,
// Dune11, reworked vlog). Talk to Spiros (203111), scout the citadel by entering any of the three
// registered sensory-area zones (var0->2), collect Revolutionary Symbols and turn them in.
// Zone-mission-end/level-up gated on quest 1130.
// Note: Java's onEnterZoneEvent ignores the zoneName parameter entirely and just checks var==1 -
// entering any of the three registered sensory zones advances scouting (not a bug, ported as-is).
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

public sealed class _1012MaskedLoiterers : QuestHandlerBase
{
    private const int QuestIdConst = 1012;
    private const int SpirosNpc    = 203111;
    private const string SensoryZone1 = "LF1A_SENSORYAREA_Q1012_1_206004_8_210030000";
    private const string SensoryZone2 = "LF1A_SENSORYAREA_Q1012_2_206005_4_210030000";
    private const string SensoryZone3 = "LF1A_SENSORYAREA_Q1012_3_206006_6_210030000";

    private readonly IItemDao _itemDao;

    public _1012MaskedLoiterers(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(SpirosNpc).OnTalk.Add(QuestId);
        RegisterOnEnterZone(engine, SensoryZone1);
        RegisterOnEnterZone(engine, SensoryZone2);
        RegisterOnEnterZone(engine, SensoryZone3);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1130, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START && env.TargetId == SpirosNpc)
        {
            switch (DialogActionLookup.FromId(env.DialogId))
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.QUEST_SELECT when var == 2:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.QUEST_SELECT when var == 3:
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                case DialogAction.SETPRO1:
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                case DialogAction.SETPRO2:
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 3, 3, true, 5, 2034, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && env.TargetId == SpirosNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnEnterZoneAsync(QuestEnv env, string zoneName, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != 1) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }
}
