// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41553DoubleDipper.java (Cheatkiller).
// Talk to 205967 to start (grants item 182212548). Using each item in the chain
// (548->549->550->551) advances the var and swaps it for the next item; talk to 205967 again
// mid-chain (SETPRO2) to advance past var 1; return the final item (551) to turn in.
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

namespace Quest.Tiamaranta;

public sealed class _41553DoubleDipper : QuestHandlerBase
{
    private const int QuestIdConst = 41553;
    private const int StartNpc     = 205967;
    private const int FirstItemId  = 182212548;
    private const int SecondItemId = 182212549;
    private const int ThirdItemId  = 182212550;
    private const int FinalItemId  = 182212551;

    private readonly IItemDao _itemDao;

    public _41553DoubleDipper(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(FirstItemId, QuestId);
        engine.RegisterQuestItem(SecondItemId, QuestId);
        engine.RegisterQuestItem(ThirdItemId, QuestId);
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
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
            {
                await GiveQuestItemAsync(player, conn, _itemDao, FirstItemId, 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == StartNpc && entry.GetVar(0) == 1)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.SELECT_ACTION_1353)
                return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
            if (dialog == DialogAction.SETPRO2)
                return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, FinalItemId, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) == 1) return false;

        // Java changeQuestStep guards on the current var matching before applying — replicated here
        // instead of relying on QuestHandlerBase's unconditional ChangeQuestStepAsync.
        switch (itemId)
        {
            case FirstItemId when entry.GetVar(0) == 0:
                await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
                break;
            case SecondItemId when entry.GetVar(0) == 2:
                await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
                break;
            case ThirdItemId when entry.GetVar(0) == 3:
                await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
                break;
            case FirstItemId or SecondItemId or ThirdItemId:
                break; // guard failed: Java still falls through to swap the item below
            default:
                return false;
        }

        await RemoveQuestItemAsync(player, conn, _itemDao, itemId, 1, ct);
        await GiveQuestItemAsync(player, conn, _itemDao, itemId + 1, 1, ct);
        return true;
    }
}
