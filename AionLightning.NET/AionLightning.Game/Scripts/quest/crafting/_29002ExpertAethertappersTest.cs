// Port of Java data/scripts/system/handlers/quest/crafting/_29002ExpertAethertappersTest.java (Gigi).
// Talk to Baraka (204257) to start; Utana (204099) hands over the aether-tapping tool
// (122001251); Baraka then requires two gathered materials (152003007 + 152003008) in one
// hand-in. Fallthrough note: same "info dialog falls into the act dialog" idiom as _19002 —
// preserved via stacked case labels.
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

public sealed class _29002ExpertAethertappersTest : QuestHandlerBase
{
    private const int QuestIdConst = 29002;
    private const int BarakaNpc  = 204257;
    private const int UtanaNpc   = 204099;
    private const int ToolItemId = 122001251;
    private const int Material1  = 152003007;
    private const int Material2  = 152003008;

    private readonly IItemDao _itemDao;

    public _29002ExpertAethertappersTest(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        var baraka = engine.RegisterQuestNpc(BarakaNpc);
        baraka.OnQuestStart.Add(QuestId);
        baraka.OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(UtanaNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry = player.Quests.Get(QuestId);
        var status = entry?.Status ?? QuestStatus.NONE;
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (status == QuestStatus.NONE)
        {
            if (targetId != BarakaNpc) return false;
            return dialog == DialogAction.QUEST_SELECT
                ? await SendQuestDialogAsync(conn, targetObjId, 4762, ct)
                : await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (status == QuestStatus.START)
        {
            if (targetId == UtanaNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.SETPRO1:
                        if (!await GiveQuestItemAsync(player, conn, _itemDao, ToolItemId, 1, ct)) return true;
                        await ChangeQuestStepAsync(conn, entry!, 0, 1, toReward: false, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
            }
            else if (targetId == BarakaNpc && dialog == DialogAction.QUEST_SELECT)
            {
                long count1 = player.Inventory.FindByItemId(Material1)?.Count ?? 0;
                long count2 = player.Inventory.FindByItemId(Material2)?.Count ?? 0;
                if (count1 > 0 && count2 > 0)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, Material1, 1, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, Material2, 1, ct);
                    await ChangeQuestStepAsync(conn, entry!, -1, 0, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                }
                return await SendQuestDialogAsync(conn, targetObjId, 10001, ct);
            }
            return false;
        }

        if (status == QuestStatus.REWARD)
        {
            if (targetId != BarakaNpc) return false;
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
