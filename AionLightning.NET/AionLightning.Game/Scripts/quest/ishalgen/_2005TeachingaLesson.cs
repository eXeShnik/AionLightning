// Port of Java data/scripts/system/handlers/quest/ishalgen/_2005TeachingaLesson.java.
// Talk to Mijou (203540), advance to var 1, collect-check → REWARD, turn in.
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

namespace Quest.Ishalgen;

public sealed class _2005TeachingaLesson : QuestHandlerBase
{
    private const int QuestIdConst = 2005;
    private const int NpcId        = 203540;

    private readonly IItemDao _itemDao;

    public _2005TeachingaLesson(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(NpcId).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2100, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || env.TargetId != NpcId) return false;
        int var = entry.GetVar(0);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    return false;
                case DialogAction.SELECT_ACTION_1012:
                    await PlayQuestMovieAsync(conn, env.Player, 54, ct);
                    return false;
                case DialogAction.SETPRO1:
                    if (var == 0)
                    {
                        entry.SetVar(0, var + 1);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
                        return true;
                    }
                    return false;
                case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                    if (var == 1)
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 1, reward: true, checkOkId: 5, checkFailId: 1693, ct);
                    return false;
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD)
            return await SendQuestEndDialogAsync(env, conn, ct);
        return false;
    }
}
