// Port of Java data/scripts/system/handlers/quest/chantra_dredgion/_3722MyNewToy.java.
// Accept at Yannis (799069) gives item 182202194 on QUEST_ACCEPT_1; using that item removes it and
// flips straight to REWARD (Java useQuestItem(env, item, 0, 0, true) — step/nextStep both 0, so
// var0 never changes); turn in back at Yannis (item already consumed, so no second removal there).
// Java's repeat guard `qs.canRepeat()` (daily-reset re-entry) isn't ported — no QuestEntry.CanRepeat
// exists in this codebase yet (same omission as every other repeatable quest already ported here,
// e.g. danaria's _13064EmployeeAppreciationDay).
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

namespace Quest.ChantraDredgion;

public sealed class _3722MyNewToy : QuestHandlerBase
{
    private const int QuestIdConst = 3722;
    private const int Npc          = 799069;
    private const int ItemId       = 182202194;

    private readonly IItemDao _itemDao;

    public _3722MyNewToy(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Npc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Npc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(ItemId, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId != Npc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            if (env.DialogId == (int)DialogAction.QUEST_ACCEPT_1)
            {
                if (!await GiveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct)) return true;
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == Npc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != ItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, ItemId, 1, ct);
        await ChangeQuestStepAsync(conn, entry, 0, 0, toReward: true, ct);
        return true;
    }
}
