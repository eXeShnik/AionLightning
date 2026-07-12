// Port of Java data/scripts/system/handlers/quest/gelkmaros/_21460AShulacksStory.java (vlog).
// Talk to Denskel (799258) to start; relay at Dorkin (799502, SETPRO1 gives item 182209520,
// var 0->1); Chenkiki (799276, SELECT_QUEST_REWARD removes the item then flips to REWARD).
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

namespace Quest.Gelkmaros;

public sealed class _21460AShulacksStory : QuestHandlerBase
{
    private const int QuestIdConst = 21460;
    private const int DenskelNpc   = 799258;
    private const int DorkinNpc    = 799502;
    private const int ChenkikiNpc  = 799276;
    private const int ItemId       = 182209520;

    private readonly IItemDao _itemDao;

    public _21460AShulacksStory(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(DenskelNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(DenskelNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DorkinNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ChenkikiNpc).OnTalk.Add(QuestId);
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
            if (targetId == DenskelNpc)
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

            if (targetId == DorkinNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                        giveItemId: ItemId, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                return false;
            }

            if (targetId == ChenkikiNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    if (await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct))
                        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == ChenkikiNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
