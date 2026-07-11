// Port of Java data/scripts/system/handlers/quest/ishalgen/_2006HitThemWhereitHurts.java.
// Talk to Mijou (203540), collect-check → reward, loot the grain sack (700095), turn in at Ulgorn (203516).
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

public sealed class _2006HitThemWhereitHurts : QuestHandlerBase
{
    private const int QuestIdConst = 2006;
    private const int MijouNpc     = 203540;
    private const int SackObj      = 700095;
    private const int UlgornNpc    = 203516;

    private readonly IItemDao _itemDao;

    public _2006HitThemWhereitHurts(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(MijouNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SackObj).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(UlgornNpc).OnTalk.Add(QuestId);
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
            if (targetId == MijouNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                        if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                        return false;
                    case DialogAction.SETPRO1:
                        return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 1, reward: true, checkOkId: 1438, checkFailId: 1353, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }
            if (targetId == SackObj && var == 1) return true;
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == UlgornNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
