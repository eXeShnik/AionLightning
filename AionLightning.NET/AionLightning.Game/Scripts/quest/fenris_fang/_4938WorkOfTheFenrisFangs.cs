// Port of Java data/scripts/system/handlers/quest/fenris_fang/_4938WorkOfTheFenrisFangs.java
// (Nanou/vlog). Start at Kvasir (204053, plain accept, no item); an 8-npc relay chain
// (798367->798368->798369->798370->798371->798372->798373->798374, var 0->8); Balder (204075)
// requires 186000084 x1 to flip to reward (removed on success); turn in at Kvasir.
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

public sealed class _4938WorkOfTheFenrisFangs : QuestHandlerBase
{
    private const int QuestIdConst = 4938;
    private const int KvasirNpc    = 204053;
    private const int RiikaardNpc  = 798367;
    private const int HerosirNpc   = 798368;
    private const int GellnerNpc   = 798369;
    private const int NatorpNpc    = 798370;
    private const int NeedhamNpc   = 798371;
    private const int LandsbergNpc = 798372;
    private const int LevinardNpc  = 798373;
    private const int LonerganNpc  = 798374;
    private const int BalderNpc    = 204075;
    private const int HolyWaterItem = 186000084;

    private readonly IItemDao _itemDao;

    public _4938WorkOfTheFenrisFangs(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(KvasirNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(KvasirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(RiikaardNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(HerosirNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(GellnerNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(NatorpNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(NeedhamNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LandsbergNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LevinardNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LonerganNpc).OnTalk.Add(QuestId);
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
            if (targetId == RiikaardNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog is DialogAction.SETPRO1 or DialogAction.QUEST_SELECT)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }
            if (targetId == HerosirNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog is DialogAction.SETPRO2 or DialogAction.QUEST_SELECT)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return false;
            }
            if (targetId == GellnerNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 2)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog is DialogAction.SETPRO3 or DialogAction.QUEST_SELECT)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == NatorpNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 3)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog is DialogAction.SETPRO4 or DialogAction.QUEST_SELECT)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                return false;
            }
            if (targetId == NeedhamNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 4)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog is DialogAction.SETPRO5 or DialogAction.QUEST_SELECT)
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                return false;
            }
            if (targetId == LandsbergNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 5)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog is DialogAction.SETPRO6 or DialogAction.QUEST_SELECT)
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                return false;
            }
            if (targetId == LevinardNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 6)
                    return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                if (dialog is DialogAction.SETPRO7 or DialogAction.QUEST_SELECT)
                    return await DefaultCloseDialogAsync(env, conn, 6, 7, ct);
                return false;
            }
            if (targetId == LonerganNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 7)
                    return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                if (dialog is DialogAction.SETPRO8 or DialogAction.QUEST_SELECT)
                    return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
                return false;
            }
            if (targetId == BalderNpc)
            {
                switch (dialog)
                {
                    case DialogAction.QUEST_SELECT:
                        if (var == 8)
                            return await SendQuestDialogAsync(conn, targetObjId, 3739, ct);
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
            }
            return false;
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
}
