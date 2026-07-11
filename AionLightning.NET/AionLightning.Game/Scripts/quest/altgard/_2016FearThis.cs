// Port of Java data/scripts/system/handlers/quest/altgard/_2016FearThis.java (MrPoke/vlog).
// Talk to Nokir (203631, movie 63), kill 5 mobs (var 1->6), report to Shania (203621), collect
// Arachna Poison Sacs (182203018) for a toxin (182203019), then use the toxin to finish.
// Zone-mission chain, level-up gated.
// Skip vs Java: the final item-use step required standing inside DF1A_ITEMUSEAREA_Q2016 (no
// zone-shape system in this port) — the zone-membership gate is omitted, so using the toxin
// anywhere now completes the quest. Cosmetic 3s SM_ITEM_USAGE_ANIMATION delay also omitted.
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Altgard;

public sealed class _2016FearThis : QuestHandlerBase
{
    private const int QuestIdConst = 2016;
    private const int NokirNpc     = 203631;
    private const int ShaniaNpc    = 203621;
    private const int PoisonSacId  = 182203018;
    private const int ToxinItemId  = 182203019;

    private static readonly int[] _mobs = [210455, 210456, 214039, 210458, 214032];

    private readonly IItemDao _itemDao;

    public _2016FearThis(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(NokirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ShaniaNpc).OnTalk.Add(QuestId);
        foreach (int mob in _mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
        engine.RegisterQuestItem(PoisonSacId, QuestId);
        engine.RegisterQuestItem(ToxinItemId, QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

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
            switch (targetId)
            {
                case NokirNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT when var == 0:
                            return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                        case DialogAction.QUEST_SELECT when var == 6:
                            return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                        case DialogAction.SELECT_ACTION_1012:
                            await PlayQuestMovieAsync(conn, env.Player, 63, ct);
                            return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
                        case DialogAction.SETPRO1:
                            return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                        case DialogAction.SETPRO2:
                            return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
                        default:
                            return false;
                    }
                case ShaniaNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT when var == 7:
                            return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                        case DialogAction.QUEST_SELECT when var == 8:
                            return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                        case DialogAction.SETPRO3:
                            return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
                        case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                            return await CheckQuestItemsAsync(env, conn, _itemDao, 8, 10, reward: false,
                                checkOkId: 2035, checkFailId: 2120, giveItemId: ToxinItemId, giveItemCount: 1, ct);
                        case DialogAction.FINISH_DIALOG:
                            return await DefaultCloseDialogAsync(env, conn, 8, 8, ct);
                        case DialogAction.SETPRO4:
                            return await DefaultCloseDialogAsync(env, conn, 10, 10, ct);
                        default:
                            return false;
                    }
                default:
                    return false;
            }
        }
        if (entry.Status == QuestStatus.REWARD && targetId == NokirNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)_mobs, 1, 6, ct);

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ToxinItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 10) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, ToxinItemId, 1, ct);
        await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
        return true;
    }
}
