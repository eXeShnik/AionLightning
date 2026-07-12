// Port of Java data/scripts/system/handlers/quest/brusthonin/_4012TroublesomePromise.java.
// Talk to 205142 to start; the promise letter (182209005) drops 100% off 700342 (Batch 0.3
// RegisterQuestDrop) — picking it up immediately flips the quest to REWARD (Java
// defaultOnGetItemEvent(env, 0, 0, true), var stays 0); turn in at 730104.
// The var==1 CHECK_USER_HAS_QUEST_ITEM branch at 730104 is preserved 1:1 even though the
// onGetItemEvent shortcut above means var never actually reaches 1 in practice — harmless,
// faithful to the Java source.
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

namespace Quest.Brusthonin;

public sealed class _4012TroublesomePromise : QuestHandlerBase
{
    private const int QuestIdConst = 4012;
    private const int StartNpc     = 205142;
    private const int TurnInNpc    = 730104;
    private const int LootNpc      = 700342;
    private const int PromiseItemId = 182209005;

    private readonly IItemDao _itemDao;

    public _4012TroublesomePromise(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LootNpc).OnTalk.Add(QuestId);
        RegisterQuestDrop(engine, LootNpc, PromiseItemId, 1, 100);
        engine.RegisterItemGet(PromiseItemId, QuestId);
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
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == LootNpc)
                return entry.GetVar(0) == 0 && dialog == DialogAction.USE_OBJECT;

            if (targetId == TurnInNpc && entry.GetVar(0) == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (env.DialogId == (int)DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, PromiseItemId, 1, ct);
                    entry.Status = QuestStatus.REWARD;
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                }
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
            return await SendQuestEndDialogAsync(env, conn, ct);

        return false;
    }

    public override async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != PromiseItemId) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 0) return false;

        entry.Status = QuestStatus.REWARD;
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }
}
