// Port of Java data/scripts/system/handlers/quest/poeta/_1004NeutralizingOdium.java.
// Campaign: talk chains through 203082/790001, use object 700030 to take/return the odium
// sample (182200005), collect-check via quest template items, reward at 203067.
// Skips vs Java (cosmetic only): sendEmotion(STAND) after using 700030 is not sent (no base
// emotion helper); the CHECK_USER_HAS_QUEST_ITEM gate is anchored to var 3 (Java leaves it
// unvared but the flow only reaches it at var 3).
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

namespace Quest.Poeta;

public sealed class _1004NeutralizingOdium : QuestHandlerBase
{
    private const int QuestIdConst   = 1004;
    private const int StartNpcId     = 203082;
    private const int ObjectNpcId    = 700030;
    private const int ScientistNpcId = 790001;
    private const int EndNpcId       = 203067;
    private const int SampleItemId   = 182200005;
    private const int VialItemId     = 182200006;

    private readonly IItemDao _itemDao;

    public _1004NeutralizingOdium(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(StartNpcId).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ObjectNpcId).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ScientistNpcId).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EndNpcId).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1100, isZoneMission: true, ct);

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
            if (targetId == StartNpcId)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.QUEST_SELECT when var == 5:
                        return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                    case DialogAction.SELECT_ACTION_1013:
                        if (var == 0)
                            await PlayQuestMovieAsync(conn, player, 19, ct);
                        return false;
                    case DialogAction.SETPRO1:
                        entry.SetVar(0, var + 1);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    case DialogAction.SETPRO3 when var == 5:
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }

            if (targetId == ObjectNpcId && (var == 1 || var == 4))
            {
                if (dialog == DialogAction.USE_OBJECT)
                {
                    if (var == 1)
                    {
                        if (await GiveQuestItemAsync(player, conn, _itemDao, SampleItemId, 1, ct))
                            entry.SetVar(0, var + 1);
                    }
                    else // var == 4
                    {
                        entry.SetVar(0, var + 1);
                        await RemoveQuestItemAsync(player, conn, _itemDao, SampleItemId, 1, ct);
                    }
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return false;
                }
                return false;
            }

            if (targetId == ScientistNpcId)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 2:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.QUEST_SELECT when var == 3:
                        return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    case DialogAction.QUEST_SELECT when var == 11:
                        return await SendQuestDialogAsync(conn, targetObjId, 1694, ct);
                    case DialogAction.SETPRO2 when var == 2:
                        entry.SetVar(0, var + 1);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    case DialogAction.SETPRO3 when var == 11:
                        if (!await GiveQuestItemAsync(player, conn, _itemDao, VialItemId, 1, ct))
                            return true;
                        entry.SetVar(0, 4);
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        await RemoveQuestItemAsync(player, conn, _itemDao, VialItemId, 1, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                        return await CheckQuestItemsAsync(env, conn, _itemDao,
                            step: 3, nextStep: 11, reward: false, checkOkId: 1694, checkFailId: 1779,
                            giveItemId: 0, giveItemCount: 0, ct);
                    default:
                        return false;
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == EndNpcId)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
