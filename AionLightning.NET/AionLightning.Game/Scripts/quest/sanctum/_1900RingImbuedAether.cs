// Port of Java data/scripts/system/handlers/quest/sanctum/_1900RingImbuedAether.java (Mr. Poke, Dune11).
// Talk to 203757 to start (gives quest item 182206003); chain through 203739 (var 0->1), 203766
// (var 1->2), 203797 (var 2->3), 203795 (var 3->reward, var left at 3); turn in at 203830
// (removes the item on the reward-select click).
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

namespace Quest.Sanctum;

public sealed class _1900RingImbuedAether : QuestHandlerBase
{
    private const int QuestIdConst = 1900;
    private const int StartNpc     = 203757;
    private const int SecondNpc    = 203739;
    private const int ThirdNpc     = 203766;
    private const int FourthNpc    = 203797;
    private const int FifthNpc     = 203795;
    private const int TurnInNpc    = 203830;
    private const int RingItemId   = 182206003;

    private readonly IItemDao _itemDao;

    public _1900RingImbuedAether(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SecondNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ThirdNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FourthNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FifthNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, RingItemId, 1, ct)) return true;
                    return await SendQuestStartDialogAsync(env, conn, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == SecondNpc)
        {
            if (entry is { Status: QuestStatus.START } && entry.GetVar(0) == 0)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1) return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == ThirdNpc)
        {
            if (entry is { Status: QuestStatus.START } && entry.GetVar(0) == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2) return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == FourthNpc)
        {
            if (entry is { Status: QuestStatus.START } && entry.GetVar(0) == 2)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO3) return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == FifthNpc)
        {
            if (entry is { Status: QuestStatus.START } && entry.GetVar(0) == 3)
            {
                if (dialog == DialogAction.QUEST_SELECT) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO4) return await DefaultCloseDialogAsync(env, conn, 3, 0, reward: true, sameNpc: false, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT && entry.Status == QuestStatus.REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD && entry.Status is not (QuestStatus.COMPLETE or QuestStatus.NONE))
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, RingItemId, 1, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
