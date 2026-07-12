// Port of Java data/scripts/system/handlers/quest/sanctum/_3970KinahDiggingDaughter.java (Cheatkiller).
// Talk to 203893 to start (gives item 182206112); chain swaps the delivery item at each stop --
// 798072 (var 0->1, 182206112->182206113), 279020 (var 1->2, 182206113->182206114), 798053
// (var 2->3, 182206114->182206115, reward); turn in at 798386 (removes 182206115).
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

public sealed class _3970KinahDiggingDaughter : QuestHandlerBase
{
    private const int QuestIdConst = 3970;
    private const int StartNpc     = 203893;
    private const int SecondNpc    = 798072;
    private const int ThirdNpc     = 279020;
    private const int FourthNpc    = 798053;
    private const int TurnInNpc    = 798386;
    private const int FirstItemId  = 182206112;
    private const int SecondItemId = 182206113;
    private const int ThirdItemId  = 182206114;
    private const int FourthItemId = 182206115;

    private readonly IItemDao _itemDao;

    public _3970KinahDiggingDaughter(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
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
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, FirstItemId, 1, ct)) return true;
                    return await SendQuestStartDialogAsync(env, conn, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == SecondNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, FirstItemId, 1, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, SecondItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
            }
            else if (targetId == ThirdNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO2)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, SecondItemId, 1, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, ThirdItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
            }
            else if (targetId == FourthNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, ThirdItemId, 1, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, FourthItemId, 1, ct);
                    entry.SetVar(0, 3);
                    return await DefaultCloseDialogAsync(env, conn, 3, 3, reward: true, sameNpc: false, ct);
                }
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, FourthItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
