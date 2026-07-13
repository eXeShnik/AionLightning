// Port of Java data/scripts/system/handlers/quest/brusthonin/_2092GravesoftheRedSkyLegion.java.
// Chain: talk Surt (205150, var 0->1) -> use object 700394 (var 1->2) -> talk 205188 (var 2->3)
// -> talk 205190 (var 3->4 -> collect-check gives item 182209009, var 4->5) -> talk any of
// 205208/205209/205210/205212/205213/205214 (var 5->6, removes 182209009) -> kill 214402/214403
// (var 6..20, one per kill) -> var==20 flips to REWARD -> turn in at Surt.
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

namespace Quest.Brusthonin;

public sealed class _2092GravesoftheRedSkyLegion : QuestHandlerBase
{
    private const int QuestIdConst = 2092;
    private const int SurtNpc      = 205150;
    private const int ObjectNpc    = 700394;
    private const int NeligorNpc   = 205188;
    private const int BuBuChanNpc  = 205190;
    private const int RedLegion1   = 214402;
    private const int RedLegion2   = 214403;
    private const int LegionKeyItem = 182209009;

    private static readonly int[] _turnInNpcs = [205208, 205209, 205210, 205212, 205213, 205214];
    private static readonly int[] _dialogsByNpc =
    [
        // parallel to _turnInNpcs
        2717, 2802, 2887, 3143, 3058, 2972,
    ];

    private readonly IItemDao _itemDao;

    public _2092GravesoftheRedSkyLegion(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(RedLegion1).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(RedLegion2).OnKill.Add(QuestId);
        engine.RegisterQuestNpc(SurtNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(NeligorNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ObjectNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BuBuChanNpc).OnTalk.Add(QuestId);
        foreach (int npc in _turnInNpcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2091, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != RedLegion1 && env.TargetId != RedLegion2) return false;

        int var = entry.GetVar(0);
        if (var >= 6 && var < 20)
        {
            await ChangeQuestStepAsync(conn, entry, 0, var + 1, toReward: false, ct);
            return true;
        }
        if (var == 20)
        {
            await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
            return true;
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != SurtNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == SurtNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.SELECT_ACTION_1012:
                    await PlayQuestMovieAsync(conn, player, 395, ct);
                    return false;
                case DialogAction.SETPRO1 when var == 0:
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (targetId == ObjectNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await UseQuestObjectAsync(env, conn, step: 1, nextStep: 2, reward: false, varNum: 0, ct);
            return false;
        }

        if (targetId == NeligorNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 2:
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                case DialogAction.SETPRO3 when var == 2:
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (targetId == BuBuChanNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 3:
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                case DialogAction.QUEST_SELECT when var == 4:
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                case DialogAction.SETPRO4 when var == 3:
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                case DialogAction.SETPRO5 when var == 4:
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                case DialogAction.CHECK_USER_HAS_QUEST_ITEM when var == 4:
                    return await CheckQuestItemsAsync(env, conn, _itemDao,
                        step: 4, nextStep: 5, reward: false, checkOkId: 10000, checkFailId: 10001,
                        giveItemId: LegionKeyItem, giveItemCount: 1, ct);
                default:
                    return false;
            }
        }

        for (int i = 0; i < _turnInNpcs.Length; i++)
        {
            if (targetId != _turnInNpcs[i]) continue;

            if (dialog == DialogAction.QUEST_SELECT && var == 5)
                return await SendQuestDialogAsync(conn, targetObjId, _dialogsByNpc[i], ct);
            if (dialog == DialogAction.SETPRO6 && var == 5)
                return await DefaultCloseDialogAsync(env, conn, _itemDao,
                    step: 5, nextStep: 6, reward: false, sameNpc: false,
                    giveItemId: 0, giveItemCount: 0, removeItemId: LegionKeyItem, removeItemCount: 1, ct);
            return false;
        }

        return false;
    }
}
