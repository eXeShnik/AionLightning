// Port of Java data/scripts/system/handlers/quest/altgard/_2020KeepingtheBlackClawTribeinCheck.java
// (MrPoke). Talk to 203665, then 203668, kill Black Claw mobs (210562/216914, var 2->5), collect
// check, turn in. Zone-mission chain, level-up gated.
using System.Collections.Generic;
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

public sealed class _2020KeepingtheBlackClawTribeinCheck : QuestHandlerBase
{
    private const int QuestIdConst = 2020;
    private const int FirstNpc     = 203665;
    private const int SecondNpc    = 203668;

    private static readonly int[] _mobs = [210562, 216914];

    private readonly IItemDao _itemDao;

    public _2020KeepingtheBlackClawTribeinCheck(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(FirstNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        foreach (int mob in _mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
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
                case FirstNpc:
                    if (dialog == DialogAction.QUEST_SELECT && var == 0)
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (dialog == DialogAction.SETPRO1)
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    return false;
                case SecondNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT when var == 1:
                            return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                        case DialogAction.QUEST_SELECT when var == 5:
                            return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                        case DialogAction.QUEST_SELECT when var == 6:
                            return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                        case DialogAction.SETPRO2 or DialogAction.SETPRO3:
                            if (await DefaultCloseDialogAsync(env, conn, 1, 2, ct)) return true;
                            return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                        case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                            return await CheckQuestItemsAsync(env, conn, _itemDao, 6, 6, reward: true, checkOkId: 5, checkFailId: 2120, ct);
                        default:
                            return false;
                    }
                default:
                    return false;
            }
        }
        if (entry.Status == QuestStatus.REWARD && targetId == SecondNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);
        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)_mobs, 2, 5, ct);
}
