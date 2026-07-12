// Port of Java data/scripts/system/handlers/quest/tiamaranta/_41556SecretsOfMorale.java
// (Cheatkiller). Talk to 205953 to start; kill any of the 4 drakans 6 times (tracked in var 1),
// one further drakan kill flips var 0 to 1; hand in the quest_data.xml collect-items at 205953 to
// receive item 182212554 (var 1->2); using that item flips straight to REWARD; turn in at 205953.
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

namespace Quest.Tiamaranta;

public sealed class _41556SecretsOfMorale : QuestHandlerBase
{
    private const int QuestIdConst  = 41556;
    private const int StartNpc      = 205953;
    private const int MoraleItemId = 182212554;

    private static readonly int[] Drakans = [218524, 218523, 218525, 218526];

    private readonly IItemDao _itemDao;

    public _41556SecretsOfMorale(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(MoraleItemId, QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        foreach (int npc in Drakans)
            engine.RegisterQuestNpc(npc).OnKill.Add(QuestId);
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
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == StartNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, reward: false,
                    checkOkId: 10000, checkFailId: 10001, giveItemId: MoraleItemId, giveItemCount: 1, ct);
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == StartNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (entry.GetVar(0) != 0) return false;

        int killed = entry.GetVar(1);
        if (killed >= 6)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: false, ct);
            return true;
        }

        await ChangeQuestStepAsync(conn, entry, 1, killed + 1, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != MoraleItemId) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, MoraleItemId, 1, ct);
        await ChangeQuestStepAsync(conn, entry, 0, 3, toReward: true, ct);
        return true;
    }
}
