// Port of Java data/scripts/system/handlers/quest/ishalgen/_2001ThinkingAhead.java.
// Talk to Boromer (203518), collect-check, kill 5 mobs (210369/210368, var 3->8), turn in.
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

namespace Quest.Ishalgen;

public sealed class _2001ThinkingAhead : QuestHandlerBase
{
    private const int QuestIdConst = 2001;
    private const int BoromerNpc   = 203518;
    private const int ObjectNpc    = 700093;
    private static readonly int[] _mobs = [210369, 210368];

    private readonly IItemDao _itemDao;

    public _2001ThinkingAhead(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(BoromerNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ObjectNpc).OnTalk.Add(QuestId);
        foreach (int mob in _mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2100, isZoneMission: true, ct);

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
            if (targetId == BoromerNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return var switch
                        {
                            0 => await SendQuestDialogAsync(conn, targetObjId, 1011, ct),
                            1 => await SendQuestDialogAsync(conn, targetObjId, 1352, ct),
                            2 => await SendQuestDialogAsync(conn, targetObjId, 1694, ct),
                            _ => false,
                        };
                    case DialogAction.SELECT_ACTION_1012:
                        await PlayQuestMovieAsync(conn, env.Player, 51, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
                    case DialogAction.SETPRO1:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    case DialogAction.SETPRO3:
                        return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, false, 1694, 1693, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }
            if (targetId == ObjectNpc) return true;
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == BoromerNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        int var = entry.GetVar(0);
        if (var is >= 3 and < 8)
            return await DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)_mobs, 3, 8, ct);
        if (var == 8)
            return await DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)_mobs, 8, reward: true, ct);
        return false;
    }
}
