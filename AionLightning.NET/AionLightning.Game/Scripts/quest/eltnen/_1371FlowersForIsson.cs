// Port of Java data/scripts/system/handlers/quest/eltnen/_1371FlowersForIsson.java (Nephis).
// Talk to 203949 to start; hand over 5 of item 152000601 (checked inline, not via quest_data.xml
// collect_items); use the flower patch object 730039 to flip straight to REWARD.
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

namespace Quest.Eltnen;

public sealed class _1371FlowersForIsson : QuestHandlerBase
{
    private const int QuestIdConst = 1371;
    private const int IssonNpc     = 203949;
    private const int FlowerObj    = 730039;
    private const int FlowerItemId = 152000601;

    private readonly IItemDao _itemDao;

    public _1371FlowersForIsson(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(IssonNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(IssonNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FlowerObj).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        int targetId = env.TargetId;
        var entry = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == IssonNpc)
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
            if (targetId == IssonNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    long itemCount = var == 0 ? player.Inventory.FindByItemId(FlowerItemId)?.Count ?? 0 : 0;
                    return itemCount > 4
                        ? await SendQuestDialogAsync(conn, targetObjId, 1353, ct)
                        : await SendQuestDialogAsync(conn, targetObjId, 1438, ct);
                }
                if (dialog == DialogAction.SETPRO1)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, FlowerItemId, 5, ct);
                    entry.SetVar(0, 2);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }
            if (targetId == FlowerObj)
                return await UseQuestObjectAsync(env, conn, step: 2, nextStep: 2, reward: true, dieObject: false, ct);
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == IssonNpc)
        {
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
