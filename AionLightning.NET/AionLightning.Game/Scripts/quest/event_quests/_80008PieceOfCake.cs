// Port of Java data/scripts/system/handlers/quest/event_quests/_80008PieceOfCake.java.
// Same targetId==0 accept idiom as _50005DaevasDayEnergy; turn-in at Ias (798415) removes the
// event cake item (182214006) before flipping to REWARD.
// See _50005 for the EventService/onLvlUpEvent skip note.
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

namespace Quest.EventQuests;

public sealed class _80008PieceOfCake : QuestHandlerBase
{
    private const int QuestIdConst = 80008;
    private const int IasNpc       = 798415;
    private const int CakeItem     = 182214006;

    private readonly IItemDao _itemDao;

    public _80008PieceOfCake(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(IasNpc).OnTalk.Add(QuestId);
        engine.RegisterOnLevelUp(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (env.TargetId == 0)
        {
            if (dialog == DialogAction.QUEST_ACCEPT_1)
            {
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                return await CloseDialogWindowAsync(conn, 0, ct);
            }
            return false;
        }

        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetObjId = env.Target?.ObjectId ?? 0;

        if (entry.Status == QuestStatus.START && env.TargetId == IasNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);

            // Java switch fallthrough: QUEST_SELECT with var != 0 (never reached at var 0) and
            // SELECT_QUEST_REWARD both fall into the same remove-item + close-dialog body.
            if (dialog is DialogAction.QUEST_SELECT or DialogAction.SELECT_QUEST_REWARD)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, CakeItem, 1, ct);
                return await DefaultCloseDialogAsync(env, conn, 0, 1, reward: true, sameNpc: true, ct);
            }
        }

        return await SendQuestRewardDialogAsync(env, conn, ct);
    }

    /// <summary>Java sendQuestRewardDialog(env, 798415, 0): reportDialogId 0 means the USE_OBJECT
    /// branch never fires, so a REWARD-status turn-in always finishes the quest directly.</summary>
    private async ValueTask<bool> SendQuestRewardDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.REWARD } || env.TargetId != IasNpc) return false;
        return await SendQuestEndDialogAsync(env, conn, ct);
    }
}
