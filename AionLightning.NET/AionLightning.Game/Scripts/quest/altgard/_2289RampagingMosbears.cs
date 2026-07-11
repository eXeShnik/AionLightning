// Port of Java data/scripts/system/handlers/quest/altgard/_2289RampagingMosbears.java (vlog).
// Talk to Gefion (203616), kill mosbears (210564/210584, var 0->5, movie 62), collect-check at
// Skanin (203618), turn in at Gefion.
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

public sealed class _2289RampagingMosbears : QuestHandlerBase
{
    private const int QuestIdConst = 2289;
    private const int GefionNpc    = 203616;
    private const int SkaninNpc    = 203618;
    private const int GivenItemId  = 182203017;

    private static readonly int[] _mobs = [210564, 210584];

    private readonly IItemDao _itemDao;

    public _2289RampagingMosbears(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(GefionNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(GefionNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SkaninNpc).OnTalk.Add(QuestId);
        foreach (int mob in _mobs)
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == GefionNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == GefionNpc)
            {
                switch (dialog)
                {
                    case DialogAction.SETPRO1:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    case DialogAction.QUEST_SELECT when var == 5:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.QUEST_SELECT when var == 7:
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    case DialogAction.SELECT_ACTION_1354:
                        await PlayQuestMovieAsync(conn, env.Player, 62, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 1354, ct);
                    case DialogAction.SETPRO2:
                        return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 7, 7, reward: true, checkOkId: 5, checkFailId: 2120, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await DefaultCloseDialogAsync(env, conn, 7, 7, ct);
                    default:
                        return false;
                }
            }
            if (targetId == SkaninNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 6)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 6, 7, reward: false, sameNpc: false,
                        giveItemId: GivenItemId, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == GefionNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnKillEventAsync(env, conn, (IReadOnlyCollection<int>)_mobs, 0, 5, ct);
}
