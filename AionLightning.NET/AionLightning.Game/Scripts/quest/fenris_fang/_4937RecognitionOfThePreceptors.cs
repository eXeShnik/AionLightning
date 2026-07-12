// Port of Java data/scripts/system/handlers/quest/fenris_fang/_4937RecognitionOfThePreceptors.java
// (Nanou/vlog/Eloann). Start at Kvasir (204053, hands out 182207112 x1 on accept via Java's
// item-giving sendQuestStartDialog overload — QuestHandlerBase.SendQuestStartDialogAsync only
// implements the no-item variant, so the grant is inlined here); a 6-npc relay chain
// (204059->204058->204057->204056->801222->801223, var 0->6, 801223 swaps 182207112 for 182207113);
// Balder (204075) requires holding 182207113 to show the finish dialog, then a purification ritual
// flips to reward; Kvasir's REWARD-status turn-in re-checks (and removes) 182207113 before allowing
// the final SendQuestEndDialogAsync — Java's plain 3-arg checkItemExistence(itemId,count,remove).
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

public sealed class _4937RecognitionOfThePreceptors : QuestHandlerBase
{
    private const int QuestIdConst = 4937;
    private const int KvasirNpc    = 204053;
    private const int FreyrNpc     = 204059;
    private const int SifNpc       = 204058;
    private const int SigynNpc     = 204057;
    private const int TraufnirNpc  = 204056;
    private const int HadubrantNpc = 801222;
    private const int BrynhildeNpc = 801223;
    private const int BalderNpc    = 204075;
    private const int StartItem    = 182207112;
    private const int RitualItem   = 182207113;
    private const int HolyWaterItem = 186000084;

    private readonly IItemDao _itemDao;

    public _4937RecognitionOfThePreceptors(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(KvasirNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(KvasirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(FreyrNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SifNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SigynNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TraufnirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HadubrantNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BrynhildeNpc).OnTalk.Add(QuestId);
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

            bool ok = await SendQuestStartDialogAsync(env, conn, ct);
            if (ok && player.Quests.Get(QuestId) is not null)
                await GiveQuestItemAsync(player, conn, _itemDao, StartItem, 1, ct);
            return ok;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == FreyrNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == SifNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog is DialogAction.SETPRO2 or DialogAction.QUEST_SELECT)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == SigynNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog is DialogAction.SETPRO3 or DialogAction.QUEST_SELECT)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == TraufnirNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog is DialogAction.SETPRO4 or DialogAction.QUEST_SELECT)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }
            if (targetId == HadubrantNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog is DialogAction.SETPRO5 or DialogAction.QUEST_SELECT)
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                return false;
            }
            if (targetId == BrynhildeNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 5)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog is DialogAction.SETPRO6 or DialogAction.QUEST_SELECT)
                    return await DefaultCloseDialogAsync(env, conn, _itemDao, 5, 6, reward: false, sameNpc: false,
                        giveItemId: RitualItem, giveItemCount: 1, removeItemId: StartItem, removeItemCount: 1, ct);
                return false;
            }
            if (targetId == BalderNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        if (var == 6 && HasItem(player, RitualItem, 1))
                            return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                        return false;
                    case DialogAction.FINISH_DIALOG:
                        return await DefaultCloseDialogAsync(env, conn, var, var, ct);
                    case DialogAction.SET_SUCCEED:
                        return await CheckItemExistenceAsync(env, conn, 6, 6, reward: true, HolyWaterItem, 1, remove: true, 0, 2461, 0, 0, ct);
                    default:
                        return false;
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == KvasirNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5, ct);

            if (await TryRemoveItemAsync(player, conn, RitualItem, 1, ct))
                return await SendQuestEndDialogAsync(env, conn, ct);
            return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
        }
        return false;
    }

    private static bool HasItem(Player player, int itemId, long count)
    {
        var item = player.Inventory.FindByItemId(itemId);
        return item is not null && item.Count >= count;
    }

    /// <summary>Java QuestHandler.checkItemExistence(itemId, itemCount, remove) — plain existence/removal
    /// gate distinct from the quest_data.xml-driven collect check.</summary>
    private async ValueTask<bool> TryRemoveItemAsync(Player player, GsClientConnection conn, int itemId, long itemCount, CancellationToken ct)
    {
        if (!HasItem(player, itemId, itemCount)) return false;
        return await RemoveQuestItemAsync(player, conn, _itemDao, itemId, itemCount, ct);
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
