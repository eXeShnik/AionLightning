// Port of Java data/scripts/system/handlers/quest/katalam/_10085RecordsRestored.java (tyrto).
// Preceding quest 10084 unlocks this on level-up (10084 itself is deferred — 205-line
// escort/follow script). Talk chain 800541 -> (use item 182215226) -> 800560 -> turn in at
// 800566. Skip vs Java: registerQuestNpc(206285).addOnAtDistanceEvent(questId) is a dead
// registration in the Java source — no onAtDistanceEvent override exists in this class — so it
// is dropped here without any behavior loss.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Katalam;

public sealed class _10085RecordsRestored : QuestHandlerBase
{
    private const int QuestIdConst  = 10085;
    private const int FirstNpc      = 800541;
    private const int SecondNpc     = 800560;
    private const int TurnInNpc     = 800566;
    private const int LetterItemId  = 182215226;
    private const int SealItemId    = 182215225;

    private readonly IItemDao _itemDao;

    public _10085RecordsRestored(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(LetterItemId, QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npcId in new[] { FirstNpc, SecondNpc, TurnInNpc })
            engine.RegisterQuestNpc(npcId).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 10084, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is not null && entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == FirstNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    await GiveQuestItemAsync(player, conn, _itemDao, LetterItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                }
                if (dialog == DialogAction.SETPRO3)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, LetterItemId, 1, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, SealItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                }
                return false;
            }
            if (targetId == SecondNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, SealItemId, 1, ct);
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, reward: true, sameNpc: false, ct);
                }
                return false;
            }
        }
        else if (entry is not null && entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.GetVar(0) != 1) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }
}
