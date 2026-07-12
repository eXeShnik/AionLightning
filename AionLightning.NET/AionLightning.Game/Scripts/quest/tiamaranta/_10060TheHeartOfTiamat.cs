// Port of Java data/scripts/system/handlers/quest/tiamaranta/_10060TheHeartOfTiamat.java (Luzien).
// Talk to Skafir (205842) to advance var 0->1 (SETPRO1); at Garnon (800018), CHECK_USER_HAS_QUEST_ITEM
// collects the quest_data.xml items and gives 182212555 (var ->2, dialog 10000), SETPRO2 then closes
// the dialog (var 1->2); turn in at Garnon once in REWARD.
// Skip vs Java: onItemUseEvent's 3s SM_ITEM_USAGE_ANIMATION delay before the item is consumed and the
// step advances is omitted (applies immediately, same precedent as _41573ACrystalofMamutProportions);
// the 701119 walking npc + two hostile 218822 escorts are still spawned, but the raw NPC-id despawn
// scan 30s later has no equivalent (no despawn-by-id API is ported - see migration_plan.md's dieObject
// note on UseQuestObjectAsync for the same gap) so the escorts are left standing, which is cosmetic.
// Java bug: the inner `case QUEST_SELECT: {...} case CHECK_USER_HAS_QUEST_ITEM: {...} case SETPRO2:`
// switch at Garnon relies on fallthrough (no break after either if-block) so a failed item check
// silently also runs the SETPRO2 close - this port keeps the three dialog actions independent instead
// (same precedent as _29071DispatchtoAltgard), which only drops that obscure double-fallthrough edge.
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

public sealed class _10060TheHeartOfTiamat : QuestHandlerBase
{
    private const int QuestIdConst = 10060;
    private const int SkafirNpc    = 205842;
    private const int GarnonNpc    = 800018;
    private const int GiftItemId   = 182212555;
    private const int KeyItemId    = 182212591;
    private const string ItemUseZone = "LDF4B_ITEMUSEAREA_Q10060A";

    private readonly IItemDao _itemDao;

    public _10060TheHeartOfTiamat(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(GiftItemId, QuestId);
        engine.RegisterQuestNpc(SkafirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GarnonNpc).OnTalk.Add(QuestId);
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

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == SkafirNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            else if (targetId == GarnonNpc)
            {
                bool hasKey = (player.Inventory.FindByItemId(KeyItemId)?.Count ?? 0) == 1;
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (hasKey) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, var, var + 1, reward: false,
                        checkOkId: 10000, checkFailId: 0, giveItemId: GiftItemId, giveItemCount: 1, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            }
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
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (itemId != GiftItemId || !player.CurrentZones.Contains(ItemUseZone)) return false;
        if (entry.GetVar(0) != 3) return false;

        var pos = player.Position;
        SpawnQuestNpc(pos.WorldId, pos.InstanceId, 701119, pos.X, pos.Y, pos.Z, (byte)pos.Heading);
        SpawnQuestNpc(pos.WorldId, pos.InstanceId, 218822, pos.X + 3, pos.Y - 3, pos.Z, (byte)pos.Heading);
        SpawnQuestNpc(pos.WorldId, pos.InstanceId, 218822, pos.X - 3, pos.Y + 3, pos.Z, (byte)pos.Heading);

        await RemoveQuestItemAsync(player, conn, _itemDao, GiftItemId, 1, ct);
        await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: true, ct);
        return true;
    }
}
