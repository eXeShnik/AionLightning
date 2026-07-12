// Port of Java data/scripts/system/handlers/quest/theobomos/_3087DivingForTreasure.java.
// Talk to the start npc (798201) to start; using the sunken chest object (700419) gives the
// Waterlogged Chest (182208063, if not already held) and flips straight to REWARD; turn in at
// the reward npc (798144).
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

namespace Quest.Theobomos;

public sealed class _3087DivingForTreasure : QuestHandlerBase
{
    private const int QuestIdConst = 3087;
    private const int StartNpc     = 798201;
    private const int ChestObject  = 700419;
    private const int RewardNpc    = 798144;
    private const int ChestItemId  = 182208063;

    private readonly IItemDao _itemDao;

    public _3087DivingForTreasure(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ChestObject).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RewardNpc).OnTalk.Add(QuestId);
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
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == ChestObject)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return entry.GetVar(0) == 0 && await SendQuestDialogAsync(conn, targetObjId, 1352, ct);

            if (dialog == DialogAction.SETPRO1)
            {
                long count = player.Inventory.FindByItemId(ChestItemId)?.Count ?? 0;
                if (count == 0 && !await GiveQuestItemAsync(player, conn, _itemDao, ChestItemId, 1, ct))
                    return true;

                entry.SetVar(0, entry.GetVar(0) + 1);
                entry.Status = QuestStatus.REWARD;
                await UpdateQuestStatusAsync(conn, entry, ct);
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == RewardNpc)
        {
            if (env.DialogId == (int)DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
