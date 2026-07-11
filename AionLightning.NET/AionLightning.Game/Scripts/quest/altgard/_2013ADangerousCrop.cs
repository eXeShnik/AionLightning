// Port of Java data/scripts/system/handlers/quest/altgard/_2013ADangerousCrop.java (MrPoke/vlog).
// Talk to Loriniah (203605), burn MuMu Carts (700096) via useQuestObject, kill/report; movie 61.
// Zone-mission chain: preceded by 2012, also level-up gated.
// Skip vs Java: entering the MUMU_FARMLAND_220030000 zone used to bump var 1->2 (no zone-shape
// system in this port) — folded into the SETPRO1 dialog transition, which now advances var 0
// straight to 2 instead of stopping at the zone-gated intermediate var 1. Same end state reached
// once the player talks to Loriniah, just without requiring a trip into the farmland polygon first.
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

public sealed class _2013ADangerousCrop : QuestHandlerBase
{
    private const int QuestIdConst = 2013;
    private const int LoriniahNpc  = 203605;
    private const int CartNpc      = 700096;
    private const int SackItemId   = 182203012;

    private readonly IItemDao _itemDao;

    public _2013ADangerousCrop(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(LoriniahNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CartNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, precedingQuestId: 2012, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2200, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;
        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == LoriniahNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.QUEST_SELECT when var == 2:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.QUEST_SELECT when var == 8:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.QUEST_SELECT when var == 9:
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    case DialogAction.SELECT_ACTION_1354:
                        await PlayQuestMovieAsync(conn, env.Player, 61, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1354, ct);
                    case DialogAction.SETPRO1:
                        return await DefaultCloseDialogAsync(env, conn, 0, 2, ct); // 0 -> 2 (zone-gate collapsed)
                    case DialogAction.SETPRO2:
                        return await DefaultCloseDialogAsync(env, conn, _itemDao, 2, 3, reward: false, sameNpc: false,
                            giveItemId: SackItemId, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                    case DialogAction.SETPRO3:
                        return await DefaultCloseDialogAsync(env, conn, _itemDao, 8, 9, reward: false, sameNpc: false,
                            giveItemId: 0, giveItemCount: 0, removeItemId: SackItemId, removeItemCount: 1, ct);
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 9, 9, reward: true, checkOkId: 5, checkFailId: 2120, ct);
                    default:
                        return false;
                }
            }
            if (targetId == CartNpc && dialog == DialogAction.USE_OBJECT)
            {
                if (var is >= 3 and < 5)
                    return await UseQuestObjectAsync(env, conn, var, var + 1, reward: false, dieObject: true, ct);
                if (var == 5)
                    return await UseQuestObjectAsync(env, conn, 5, 8, reward: false, dieObject: true, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == LoriniahNpc)
        {
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
