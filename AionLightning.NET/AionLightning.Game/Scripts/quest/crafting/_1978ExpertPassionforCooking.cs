// Port of Java data/scripts/system/handlers/quest/crafting/_1978ExpertPassionforCooking.java (Gigi).
// Single-npc "hand in the crafted proof items" quest: talk to Hestia (203784), turn in all three
// of 182206919/182206920/182206921 to flip to REWARD, then collect the standard quest reward.
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

namespace Quest.Crafting;

public sealed class _1978ExpertPassionforCooking : QuestHandlerBase
{
    private const int QuestIdConst = 1978;
    private const int HestiaNpc = 203784;
    private const int ProofItem1 = 182206919;
    private const int ProofItem2 = 182206920;
    private const int ProofItem3 = 182206921;

    private readonly IItemDao _itemDao;

    public _1978ExpertPassionforCooking(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        var hestia = engine.RegisterQuestNpc(HestiaNpc);
        hestia.OnQuestStart.Add(QuestId);
        hestia.OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        var status = entry?.Status ?? QuestStatus.NONE;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId != HestiaNpc) return false;

        if (status == QuestStatus.NONE)
        {
            return dialog == DialogAction.QUEST_SELECT
                ? await SendQuestDialogAsync(conn, targetObjId, 1011, ct)
                : await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (status == QuestStatus.START && dialog == DialogAction.QUEST_SELECT)
        {
            long count1 = player.Inventory.FindByItemId(ProofItem1)?.Count ?? 0;
            long count2 = player.Inventory.FindByItemId(ProofItem2)?.Count ?? 0;
            long count3 = player.Inventory.FindByItemId(ProofItem3)?.Count ?? 0;
            if (count1 <= 0 || count2 <= 0 || count3 <= 0)
                return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);

            await RemoveQuestItemAsync(player, conn, _itemDao, ProofItem1, 1, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, ProofItem2, 1, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, ProofItem3, 1, ct);
            await ChangeQuestStepAsync(conn, entry!, -1, 0, toReward: true, ct);
            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
        }

        if (status == QuestStatus.REWARD)
        {
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
