// Port of Java data/scripts/system/handlers/quest/eltnen/_1033SatalocasHeart.java (Xitanium, reworked vlog, modified apozema).
// Zone-mission quest, part of the Kaidan Fortress chain (1300): talk to Diomedes (203900, var 0->1,
// movie 178); Kimeia (203996) starts a 180s brewing timer (var 1->10, movie 42), which advances to
// var 11 ("ready") on expiry; submitting then checks the player's Drake Fang count (182201019) -
// fewer than 5 reverts to var 1 (retry), 5-6 or 7+ flips to REWARD at a different tier (var 12/13);
// turn in at Diomedes, picking the matching reward tier.
// Skip vs Java: onLogOutEvent (resets var 10 back to 1 if the player disconnects during the timed
// brewing window) has no OnLogOut hook in this port - omitted, harmless to completion (see
// migration_plan.md).
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

namespace Quest.Eltnen;

public sealed class _1033SatalocasHeart : QuestHandlerBase
{
    private const int QuestIdConst = 1033;
    private const int DiomedesNpc  = 203900;
    private const int KimeiaNpc    = 203996;
    private const int DrakeFangItem = 182201019;

    private readonly IItemDao _itemDao;

    public _1033SatalocasHeart(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnQuestTimerEnd(QuestId);
        engine.RegisterQuestNpc(DiomedesNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(KimeiaNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == DiomedesNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SELECT_ACTION_1013 && var == 0)
                {
                    await PlayQuestMovieAsync(conn, player, 178, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1013, ct);
                }
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == KimeiaNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.QUEST_SELECT when var >= 10:
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    case DialogAction.SELECT_ACTION_1695:
                        await PlayQuestMovieAsync(conn, player, 42, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1695, ct);
                    case DialogAction.SETPRO3:
                        StartQuestTimer(env, conn, 180);
                        return await DefaultCloseDialogAsync(env, conn, 1, 10, ct);
                    case DialogAction.SELECT_ACTION_2035:
                    {
                        if (var != 11) return false;
                        long drakeFangs = player.Inventory.FindByItemId(DrakeFangItem)?.Count ?? 0;
                        await RemoveQuestItemAsync(player, conn, _itemDao, DrakeFangItem, drakeFangs, ct);
                        if (drakeFangs < 5)
                        {
                            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                            return await SendQuestDialogAsync(conn, targetObjId, 2035, ct);
                        }
                        if (drakeFangs < 7)
                        {
                            await ChangeQuestStepAsync(conn, entry, 0, 12, toReward: true, ct);
                            return await SendQuestDialogAsync(conn, targetObjId, 2120, ct);
                        }
                        await ChangeQuestStepAsync(conn, entry, 0, 13, toReward: true, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 2205, ct);
                    }
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }

            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == DiomedesNpc)
            {
                if (dialog == DialogAction.USE_OBJECT)
                {
                    if (var == 12) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    if (var == 13) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                    return false;
                }
                return await FinishQuestAsync(conn, player, var - 12, ct);
            }
            if (targetId == KimeiaNpc && dialog == DialogAction.FINISH_DIALOG)
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 10) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 11, toReward: false, ct);
        return true;
    }
}
