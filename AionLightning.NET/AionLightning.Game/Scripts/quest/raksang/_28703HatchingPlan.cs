// Port of Java data/scripts/system/handlers/quest/raksang/_28703HatchingPlan.java (Cheatkiller).
// Asmodian mirror of _18703EggOnYourFace: item-use start (182212209, no start npc), advances via
// npc 799431 (var0->1, var2->3 using item 182212210), npc 701115 kill-drops item 182212211
// (100% chance, needed at var3), turn in at npc 798442.
// Skip vs Java: same cast-time-animation simplification as _18703EggOnYourFace.
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Raksang;

public sealed class _28703HatchingPlan : QuestHandlerBase
{
    private const int QuestIdConst = 28703;
    private const int HendianNpc   = 799431;
    private const int EggLayerNpc  = 701115;
    private const int TurnInNpc    = 798442;
    private const int OfferItem    = 182212209;
    private const int CrackedEgg   = 182212210;
    private const int VeinItem     = 182212211;

    private readonly IItemDao _itemDao;

    public _28703HatchingPlan(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(OfferItem, QuestId);
        engine.RegisterQuestItem(CrackedEgg, QuestId);
        engine.RegisterQuestNpc(HendianNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EggLayerNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(TurnInNpc).OnTalk.Add(QuestId);
        RegisterQuestDrop(engine, EggLayerNpc, VeinItem, 1, 100);
        engine.RegisterItemGet(VeinItem, QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == 0 && dialog == DialogAction.QUEST_ACCEPT_1)
            {
                await RemoveQuestItemAsync(player, conn, _itemDao, OfferItem, 1, ct);
                await GiveQuestItemAsync(player, conn, _itemDao, CrackedEgg, 1, ct);
                await StartMissionAsync(conn, player, QuestStatus.START, ct);
                await conn.SendAsync(new SM_DIALOG_WINDOW(0, 0), ct);
                return true;
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == HendianNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return false;
            }
            if (targetId == EggLayerNpc)
                return var == 3;
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == TurnInNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, VeinItem, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != VeinItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 3) return false;

        await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
        return true;
    }

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        var entry = player.Quests.Get(QuestId);
        if (itemId == OfferItem)
        {
            if (entry is not null && entry.Status != QuestStatus.NONE) return false;
            return await SendQuestDialogAsync(conn, 0, 4, ct);
        }

        if (itemId == CrackedEgg && entry is not null && entry.Status == QuestStatus.START && entry.GetVar(0) == 1)
        {
            var env = new QuestEnv(null, player, QuestId, 0);
            return await UseQuestObjectAsync(env, conn, 1, 2, reward: false, varNum: 0,
                addItemId: 0, addItemCount: 0, removeItemId: CrackedEgg, removeItemCount: 1,
                movieId: 0, dieObject: false, _itemDao, ct);
        }
        return false;
    }
}
