// Port of Java data/scripts/system/handlers/quest/miragent_holy_templar/_3938WellRounded.java
// (Nanou, modified by bobobear). Start at Lavirintos (203701, var 0), who offers six SETPROx
// branches (0->1..6) — one per crafting-skill path chosen by the player; each of the six craft
// masters (203788/203792/203790/203793/203784/203786) then advances that path's var (1..6) to 7,
// handing out a mark-of-mastery item; Anusis (798316) requires the "crafted heart" (186000077,
// removed) to advance 7->8; report to Jucleas (203752) with the Glossy Oath Stone (186000081,
// removed) to flip to reward; turn in at Lavirintos.
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

namespace Quest.MiragentHolyTemplar;

public sealed class _3938WellRounded : QuestHandlerBase
{
    private const int QuestIdConst   = 3938;
    private const int LavirintosNpc  = 203701;
    private const int WeaponsmithNpc = 203788;
    private const int HandicraftNpc  = 203792;
    private const int ArmorsmithNpc  = 203790;
    private const int TailoringNpc   = 203793;
    private const int CookingNpc     = 203784;
    private const int AlchemyNpc     = 203786;
    private const int AnusisNpc      = 798316;
    private const int JucleasNpc     = 203752;

    private const int WeaponsmithMark = 152201596;
    private const int HandicraftMark  = 152201639;
    private const int ArmorsmithMark  = 152201615;
    private const int TailoringMark   = 152201632;
    private const int CookingMark     = 152201644;
    private const int AlchemyMark     = 152201643;
    private const int CraftedHeartItem = 186000077;
    private const int GlossyOathStoneItem = 186000081;

    private readonly IItemDao _itemDao;

    public _3938WellRounded(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(LavirintosNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(LavirintosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(WeaponsmithNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HandicraftNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(ArmorsmithNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TailoringNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CookingNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AlchemyNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(AnusisNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(JucleasNpc).OnTalk.Add(QuestId);
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
            if (targetId != LavirintosNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == LavirintosNpc)
            {
                if (var != 0) return false;
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
            if (targetId == WeaponsmithNpc)
            {
                if (var != 1) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO7)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 1, 7, reward: false, sameNpc: false,
                        giveItemId: WeaponsmithMark, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                return false;
            }
            if (targetId == HandicraftNpc)
            {
                if (var != 2) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO7)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 2, 7, reward: false, sameNpc: false,
                        giveItemId: HandicraftMark, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                return false;
            }
            if (targetId == ArmorsmithNpc)
            {
                if (var != 3) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO7)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 3, 7, reward: false, sameNpc: false,
                        giveItemId: ArmorsmithMark, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                return false;
            }
            if (targetId == TailoringNpc)
            {
                if (var != 4) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO7)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 4, 7, reward: false, sameNpc: false,
                        giveItemId: TailoringMark, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                return false;
            }
            if (targetId == CookingNpc)
            {
                if (var != 5) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.SETPRO7)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 5, 7, reward: false, sameNpc: false,
                        giveItemId: CookingMark, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                return false;
            }
            if (targetId == AlchemyNpc)
            {
                if (var != 6) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog == DialogAction.SETPRO7)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 6, 7, reward: false, sameNpc: false,
                        giveItemId: AlchemyMark, giveItemCount: 1, removeItemId: 0, removeItemCount: 0, ct);
                return false;
            }
            if (targetId == AnusisNpc)
            {
                if (var != 7) return false;
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                    return await CheckItemExistenceAsync(env, conn, 7, 8, reward: false, CraftedHeartItem, 1, remove: true, 10000, 10001, 0, 0, ct);
                return false;
            }
            if (targetId == JucleasNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        if (var == 8)
                            return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
                        return false;
                    case DialogAction.SET_SUCCEED:
                        if (HasItem(player, GlossyOathStoneItem, 1))
                        {
                            await RemoveQuestItemAsync(player, conn, _itemDao, GlossyOathStoneItem, 1, ct);
                            return await DefaultCloseDialogAsync(env, conn, 8, 8, reward: true, sameNpc: false, ct);
                        }
                        return await SendQuestDialogAsync(conn, targetObjId, 3825, ct);
                    case DialogAction.FINISH_DIALOG:
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    default:
                        return false;
                }
            }
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.REWARD && targetId == LavirintosNpc)
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
