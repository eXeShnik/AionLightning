// Port of Java data/scripts/system/handlers/quest/sanctum/_3920TheSecretOfSurkana.java (Bobobear).
// Talk to Shoshinerk (798357) to start (gives item 182206073); use the Balaur Material Converter
// (730212) to swap it for item 182206074 and flip to REWARD; return to Shoshinerk to finish. The
// Java source also calls registerQuestItem for both items but never implements onItemUseEvent --
// dead registration carried through as-is (harmless; completion goes through the converter's
// USE_OBJECT dialog, not an item-use trigger).
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

public sealed class _3920TheSecretOfSurkana : QuestHandlerBase
{
    private const int QuestIdConst = 3920;
    private const int StartNpc     = 798357;
    private const int ConverterObj = 730212;
    private const int InactiveItemId = 182206073;
    private const int ActiveItemId   = 182206074;

    private readonly IItemDao _itemDao;

    public _3920TheSecretOfSurkana(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ConverterObj).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(InactiveItemId, QuestId);
        engine.RegisterQuestItem(ActiveItemId, QuestId);
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
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, InactiveItemId, 1, ct)) return true;
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START && targetId == ConverterObj)
        {
            if (dialog == DialogAction.USE_OBJECT && entry.GetVar(0) == 0 &&
                (player.Inventory.FindByItemId(InactiveItemId)?.Count ?? 0) > 0)
            {
                return await UseQuestObjectAsync(env, conn, 0, 1, reward: true, varNum: 0,
                    addItemId: ActiveItemId, addItemCount: 1, removeItemId: InactiveItemId, removeItemCount: 1,
                    movieId: 0, dieObject: false, _itemDao, ct);
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
