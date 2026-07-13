// Port of Java data/scripts/system/handlers/quest/empyrean_crucible/_18213TheChillingTruth.java (Cheatkiller).
// Elyos side. Started at 205985 (gives item 182212219); relay at 205316 exchanges 182212219 for
// 182212220 (var0 0->1) then the registered quest item 182212220 is used to advance var0 1->2 (Java
// onItemUseEvent, HandlerResult); final NPC 798604 advances 2->3 and grants REWARD; turn in at 798604.
// Java quirk preserved 1:1: at 798604's QUEST_SELECT dialog, if var0 is neither 1 nor 3 the switch
// statement's missing break falls through into the SETPRO2 case (defaultCloseDialog(env, 1, 2)) -
// this is the same idiom used throughout this codebase to let a stray QUEST_SELECT click reuse the
// close-dialog handler, not a bug to fix.
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

namespace Quest.EmpyreanCrucible;

public sealed class _18213TheChillingTruth : QuestHandlerBase
{
    private const int QuestIdConst = 18213;
    private const int StartNpc     = 205985;
    private const int RelayNpc     = 205316;
    private const int TurnInNpc    = 798604;
    private const int GivenItem    = 182212219;
    private const int UsedItem     = 182212220;

    private readonly IItemDao _itemDao;

    public _18213TheChillingTruth(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(UsedItem, QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RelayNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            await GiveQuestItemAsync(player, conn, _itemDao, GivenItem, 1, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == RelayNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SELECT_ACTION_1353)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, GivenItem, 1, ct);
                    await GiveQuestItemAsync(player, conn, _itemDao, UsedItem, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
                }
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            else if (targetId == TurnInNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    // Java fall-through: an unmatched QUEST_SELECT var drops into the SETPRO2 case below.
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                }
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    if (var == 3)
                        await ChangeQuestStepAsync(conn, entry, -1, 0, toReward: true, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != TurnInNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != UsedItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, UsedItem, 1, ct);
        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: false, ct);
        return true;
    }
}
