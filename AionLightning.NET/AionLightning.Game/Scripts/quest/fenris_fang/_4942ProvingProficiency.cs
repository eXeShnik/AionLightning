// Port of Java data/scripts/system/handlers/quest/fenris_fang/_4942ProvingProficiency.java
// (Nanou, modified by bobobear). Start at Kvasir (204053, plain accept); at var 0, Kvasir offers six
// SETPROx branches (0->1..6) — one per crafting-skill path chosen by the player; each of the six
// craft masters (204104/204108/204106/204110/204100/204102) then advances that path's var (1..6) to
// 7, handing out a mark-of-mastery item; Usener (798317) requires the "crafted heart" (186000077,
// removed) to advance 7->8; Balder (204075) requires 186000085 x1 to flip to reward; turn in at
// Kvasir. Java's outer switch(targetId) has NO break after the "case 204053: if (var==0){...} break;"
// block, so re-visiting Kvasir once var != 0 falls straight into the Weaponsmith (204104) branch's
// own var==1 check — reproduced with an explicit `goto case`; every craft-master case has its own
// unconditional break afterward, so the cascade goes no further than that single hop.
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

namespace Quest.FenrisFang;

public sealed class _4942ProvingProficiency : QuestHandlerBase
{
    private const int QuestIdConst   = 4942;
    private const int KvasirNpc      = 204053;
    private const int WeaponsmithNpc = 204104;
    private const int HandicraftNpc  = 204108;
    private const int ArmorsmithNpc  = 204106;
    private const int TailoringNpc   = 204110;
    private const int CookingNpc     = 204100;
    private const int AlchemyNpc     = 204102;
    private const int UsenerNpc      = 798317;
    private const int BalderNpc      = 204075;

    private const int WeaponsmithMark = 152206598;
    private const int HandicraftMark  = 152206641;
    private const int ArmorsmithMark  = 152206617;
    private const int TailoringMark   = 152206634;
    private const int CookingMark     = 152206646;
    private const int AlchemyMark     = 152206645;
    private const int CraftedHeartItem = 186000077;
    private const int HolyWaterItem   = 186000085;

    private readonly IItemDao _itemDao;

    public _4942ProvingProficiency(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(KvasirNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(KvasirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(WeaponsmithNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HandicraftNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ArmorsmithNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TailoringNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CookingNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AlchemyNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(UsenerNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BalderNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != KvasirNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            switch (targetId)
            {
                case KvasirNpc:
                    if (var == 0)
                    {
                        switch (dialog)
                        {
                            case DialogAction.QUEST_SELECT: return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                            case DialogAction.SETPRO1: return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                            case DialogAction.SETPRO2: return await DefaultCloseDialogAsync(env, conn, 0, 2, ct);
                            case DialogAction.SETPRO3: return await DefaultCloseDialogAsync(env, conn, 0, 3, ct);
                            case DialogAction.SETPRO4: return await DefaultCloseDialogAsync(env, conn, 0, 4, ct);
                            case DialogAction.SETPRO5: return await DefaultCloseDialogAsync(env, conn, 0, 5, ct);
                            case DialogAction.SETPRO6: return await DefaultCloseDialogAsync(env, conn, 0, 6, ct);
                            default: return false;
                        }
                    }
                    // Java: falls through into the Weaponsmith (204104) branch below when var != 0.
                    goto case WeaponsmithNpc;
                case WeaponsmithNpc:
                    if (var == 1)
                    {
                        if (dialog == DialogAction.QUEST_SELECT)
                            return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                        if (dialog == DialogAction.SETPRO7)
                            return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 7, reward: false, sameNpc: false,
                                giveItemId: WeaponsmithMark, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                    }
                    return false;
                case HandicraftNpc:
                    if (var == 2)
                    {
                        if (dialog == DialogAction.QUEST_SELECT)
                            return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                        if (dialog == DialogAction.SETPRO7)
                            return await DefaultCloseDialogAsync(env, conn, _itemDao, 2, 7, reward: false, sameNpc: false,
                                giveItemId: HandicraftMark, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                    }
                    return false;
                case ArmorsmithNpc:
                    if (var == 3)
                    {
                        if (dialog == DialogAction.QUEST_SELECT)
                            return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                        if (dialog == DialogAction.SETPRO7)
                            return await DefaultCloseDialogAsync(env, conn, _itemDao, 3, 7, reward: false, sameNpc: false,
                                giveItemId: ArmorsmithMark, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                    }
                    return false;
                case TailoringNpc:
                    if (var == 4)
                    {
                        if (dialog == DialogAction.QUEST_SELECT)
                            return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                        if (dialog == DialogAction.SETPRO7)
                            return await DefaultCloseDialogAsync(env, conn, _itemDao, 4, 7, reward: false, sameNpc: false,
                                giveItemId: TailoringMark, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                    }
                    return false;
                case CookingNpc:
                    if (var == 5)
                    {
                        if (dialog == DialogAction.QUEST_SELECT)
                            return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                        if (dialog == DialogAction.SETPRO7)
                            return await DefaultCloseDialogAsync(env, conn, _itemDao, 5, 7, reward: false, sameNpc: false,
                                giveItemId: CookingMark, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                    }
                    return false;
                case AlchemyNpc:
                    if (var == 6)
                    {
                        if (dialog == DialogAction.QUEST_SELECT)
                            return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                        if (dialog == DialogAction.SETPRO7)
                            return await DefaultCloseDialogAsync(env, conn, _itemDao, 6, 7, reward: false, sameNpc: false,
                                giveItemId: AlchemyMark, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                    }
                    return false;
                case UsenerNpc:
                    if (var == 7)
                    {
                        if (dialog == DialogAction.QUEST_SELECT)
                            return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                        if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                            return await CheckItemExistenceAsync(env, conn, 7, 8, reward: false, CraftedHeartItem, 1, remove: true, 10000, 10001, 0, 0, ct);
                    }
                    return false;
                case BalderNpc:
                    switch (dialog)
                    {
                        case DialogAction.QUEST_SELECT:
                            if (var == 8)
                                return await SendQuestDialogAsync(conn, targetObjId, 3740, ct);
                            return false;
                        case DialogAction.SET_SUCCEED:
                            if (HasItem(player, HolyWaterItem, 1))
                            {
                                await RemoveQuestItemAsync(player, conn, _itemDao, HolyWaterItem, 1, ct);
                                return await DefaultCloseDialogAsync(env, conn, 8, 8, reward: true, sameNpc: false, ct);
                            }
                            return await SendQuestDialogAsync(conn, targetObjId, 3825, ct);
                        case DialogAction.FINISH_DIALOG:
                            return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                        default:
                            return false;
                    }
                default:
                    return await SendQuestStartDialogAsync(env, conn, ct);
            }
        }

        if (entry.Status == QuestStatus.REWARD && targetId == KvasirNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    private static bool HasItem(Player player, int itemId, long count)
    {
        var item = player.Inventory.FindByItemId(itemId);
        return item is not null && item.Count >= count;
    }

    /// <summary>Java QuestHandler.checkItemExistence(step, nextStep, reward, itemId, itemCount, remove,
    /// checkOkId, checkFailId, giveItemId, giveItemCount) — an explicit item id/count gate, distinct
    /// from the quest_data.xml collect-items list <see cref="QuestHandlerBase.CheckQuestItemsAsync"/> reads.</summary>
    private async ValueTask<bool> CheckItemExistenceAsync(QuestEnv env, GsClientConnection conn,
        int step, int nextStep, bool reward, int itemId, long itemCount, bool remove,
        int checkOkId, int checkFailId, int giveItemId, long giveItemCount, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetObjId = env.Target?.ObjectId ?? 0;
        if (entry is null || entry.GetVar(0) != step) return false;

        bool has = HasItem(player, itemId, itemCount);
        if (has && remove)
            has = await RemoveQuestItemAsync(player, conn, _itemDao, itemId, itemCount, ct);

        if (!has)
            return await SendQuestDialogAsync(conn, targetObjId, checkFailId, ct);

        if (giveItemId != 0 && giveItemCount != 0 && !await GiveQuestItemAsync(player, conn, _itemDao, giveItemId, giveItemCount, ct))
            return false;

        await ChangeQuestStepAsync(conn, entry, 0, nextStep, reward, ct);
        return await SendQuestDialogAsync(conn, targetObjId, checkOkId, ct);
    }
}
