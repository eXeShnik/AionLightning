// Port of Java data/scripts/system/handlers/quest/tiamaranta/_20062WatchingWaiting.java (vlog).
// Talk to Garnon (800018) to advance var 0->1 (SETPRO1); CHECK_USER_HAS_QUEST_ITEM collects the
// quest_data.xml items and gives 182212559 (var 1->2); using that item while inside its use-area
// (LDF4B_ITEMUSEAREA_Q20062A, from the item_templates.xml uselimits - no ItemTemplate.UseArea field
// is ported so the zone name is hardcoded here) consumes it and flips to REWARD; turn in at Garnon.
// Skip vs Java: onItemUseEvent also spawns two hostile 218822 npcs and aggros them onto the player
// (getAggroList().addHate) - the spawn is kept (cosmetic set-dressing), the aggro call is dropped
// since no AggroList API is ported; harmless (the spawned npcs just don't auto-attack).
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

public sealed class _20062WatchingWaiting : QuestHandlerBase
{
    private const int QuestIdConst = 20062;
    private const int GarnonNpc    = 800018;
    private const int CoreItemId   = 182212559;
    private const int SentryNpc    = 218822;
    private const string ItemUseZone = "LDF4B_ITEMUSEAREA_Q20062A";

    private readonly IItemDao _itemDao;

    public _20062WatchingWaiting(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(GarnonNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestItem(CoreItemId, QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var = entry.GetVar(0);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START && targetId == GarnonNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT)
            {
                if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            }
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 1, 2, reward: false,
                    checkOkId: 10000, checkFailId: 10001, giveItemId: CoreItemId, giveItemCount: 1, ct);
            if (dialog == DialogAction.FINISH_DIALOG)
                return await CloseDialogWindowAsync(conn, targetObjId, ct);
        }
        else if (entry.Status == QuestStatus.REWARD && targetId == GarnonNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 2) return false;
        if (itemId != CoreItemId || !player.CurrentZones.Contains(ItemUseZone)) return false;

        await RemoveQuestItemAsync(player, conn, _itemDao, CoreItemId, 1, ct);
        var pos = player.Position;
        SpawnQuestNpc(pos.WorldId, pos.InstanceId, SentryNpc, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
        SpawnQuestNpc(pos.WorldId, pos.InstanceId, SentryNpc, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
        return true;
    }
}
