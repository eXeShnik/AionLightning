// Port of Java data/scripts/system/handlers/quest/crafting/_29000ExpertEssencetappersTest.java (Gigi/vlog).
// Asmodian mirror of _19000ExpertEssencetappersTest, but implemented differently in Java: Latatusk
// (204096) starts/finishes; Relir (204097) hands the tapping tool straight through
// DefaultCloseDialogAsync's item-give overload (var 0->1 immediately — no separate gather-trigger
// item-get hook like the Elyos version uses).
// Skip vs Java: Relir's SETPRO1 guards the give behind `!player.getInventory().isFullSpecialCube()`
// (a "special gathering cube" capacity gate this port has no model for). Omitted — the ordinary
// bag-space check already inside GiveQuestItemAsync/DefaultCloseDialogAsync covers "can't fit the
// item" the same way, just without the separate cube concept.
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

public sealed class _29000ExpertEssencetappersTest : QuestHandlerBase
{
    private const int QuestIdConst = 29000;
    private const int LatatuskNpc = 204096;
    private const int RelirNpc    = 204097;
    private const int ToolItemId  = 122001250;

    private readonly IItemDao _itemDao;

    public _29000ExpertEssencetappersTest(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        var latatusk = engine.RegisterQuestNpc(LatatuskNpc);
        latatusk.OnQuestStart.Add(QuestId);
        latatusk.OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelirNpc).OnTalk.Add(QuestId);
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
            if (targetId != LatatuskNpc) return false;
            return dialog == DialogAction.QUEST_SELECT
                ? await SendQuestDialogAsync(conn, targetObjId, 4762, ct)
                : await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (status == QuestStatus.START)
        {
            int var = entry!.GetVar(0);
            if (targetId == RelirNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 0:
                        return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    case DialogAction.QUEST_SELECT:
                    case DialogAction.SETPRO1:
                        return await DefaultCloseDialogAsync(env, conn, _itemDao, 0, 1, reward: false, sameNpc: false,
                            giveItemId: ToolItemId, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                }
            }
            else if (targetId == LatatuskNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT when var == 1:
                        return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    case DialogAction.QUEST_SELECT:
                    case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                        return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 1, reward: true, checkOkId: 5, checkFailId: 10001, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await DefaultCloseDialogAsync(env, conn, 1, 1, ct);
                }
            }
            return false;
        }

        if (status == QuestStatus.REWARD && targetId == LatatuskNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }
}
