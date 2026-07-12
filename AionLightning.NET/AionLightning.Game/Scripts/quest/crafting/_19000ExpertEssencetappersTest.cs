// Port of Java data/scripts/system/handlers/quest/crafting/_19000ExpertEssencetappersTest.java (Gigi/vlog).
// Talk to Cornelius (203780) to start; Sabotes (203781) hands over the essence-tapping tool
// (122001250, tracked via RegisterItemGet — var 0->1 fires the next time the player acquires
// that item id, e.g. by gathering with the tool); Cornelius then runs the quest_data.xml
// collect-item check to finish. Fallthrough note: Java's switch has no break between the
// "show info" and "act" dialog cases (QUEST_SELECT falls into SETPRO1/CHECK_USER_HAS_QUEST_ITEM
// once the var-guarded early return doesn't fire) — preserved via stacked case labels below,
// matching Java behavior exactly (harmless: SETPRO1's give is idempotent once capped at 1, and
// the item-check just reports "not ready" on a premature re-click).
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

namespace Quest.Crafting;

public sealed class _19000ExpertEssencetappersTest : QuestHandlerBase
{
    private const int QuestIdConst = 19000;
    private const int CorneliusNpc = 203780;
    private const int SabotesNpc   = 203781;
    private const int ToolItemId   = 122001250;

    private readonly IItemDao _itemDao;

    public _19000ExpertEssencetappersTest(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        var cornelius = engine.RegisterQuestNpc(CorneliusNpc);
        cornelius.OnQuestStart.Add(QuestId);
        cornelius.OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SabotesNpc).OnTalk.Add(QuestId);
        engine.RegisterItemGet(ToolItemId, QuestId);
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
            if (targetId != CorneliusNpc) return false;
            return dialog == DialogAction.QUEST_SELECT
                ? await SendQuestDialogAsync(conn, targetObjId, 4762, ct)
                : await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (status == QuestStatus.START)
        {
            int var = entry!.GetVar(0);
            if (targetId == SabotesNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.QUEST_SELECT:
                    case DialogAction.SETPRO1:
                        await GiveQuestItemAsync(player, conn, _itemDao, ToolItemId, 1, ct);
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
            }
            else if (targetId == CorneliusNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.QUEST_SELECT:
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 1, reward: true, checkOkId: 5, checkFailId: 10001, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
            }
            return false;
        }

        if (status == QuestStatus.REWARD && targetId == CorneliusNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ToolItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;
        await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
        return true;
    }
}
