// Port of Java data/scripts/system/handlers/quest/eltnen/_1466RespectForDeltras.java (Nephis and AU quest helper Team).
// Standalone quest: the Prisoner (212649) starts it, handing out the Deltras Token (182201385);
// using the token inside "EXECUTION_GROUND_OF_DELTRAS_220020000" consumes it and flips straight to
// REWARD; Telemachus (203903) finishes it.
// Skip vs Java: the 3s SM_ITEM_USAGE_ANIMATION cast delay on the token use is applied on the same
// tick instead - the same simplification UseQuestObjectAsync's doc comment already documents.
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

namespace Quest.Eltnen;

public sealed class _1466RespectForDeltras : QuestHandlerBase
{
    private const int QuestIdConst  = 1466;
    private const int PrisonerNpc   = 212649;
    private const int TelemachusNpc = 203903;
    private const int DeltrasTokenItem = 182201385;
    private const string ExecutionGroundZone = "EXECUTION_GROUND_OF_DELTRAS_220020000";

    private readonly IItemDao _itemDao;

    public _1466RespectForDeltras(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(DeltrasTokenItem, QuestId);
        engine.RegisterQuestNpc(PrisonerNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(PrisonerNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TelemachusNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != DeltrasTokenItem) return false;
        if (!player.CurrentZones.Contains(ExecutionGroundZone)) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, DeltrasTokenItem, 1, ct);
        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId == PrisonerNpc)
        {
            if (entry is null || entry.Status == QuestStatus.NONE)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_1)
                {
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, DeltrasTokenItem, 1, ct)) return true;
                    return await SendQuestStartDialogAsync(env, conn, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == TelemachusNpc && entry is not null)
        {
            if (dialog == DialogAction.QUEST_SELECT && entry.Status == QuestStatus.START)
                return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD && entry.Status is not QuestStatus.COMPLETE and not QuestStatus.NONE)
            {
                entry.SetVar(0, 2);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
