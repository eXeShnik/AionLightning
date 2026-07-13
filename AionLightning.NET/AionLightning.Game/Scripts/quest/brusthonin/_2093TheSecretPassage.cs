// Port of Java data/scripts/system/handlers/quest/brusthonin/_2093TheSecretPassage.java.
// Talk chain Surt (205150, var 0->1) -> Neligor (205159, 1->2) -> BuBu Khaaan (205164, 2->3)
// -> BuBu Chan (205197, 3->4, then collect-check 4->5) -> Cayron (205198, 5->6, movie+consume
// Book of Brohum at var 7->8) -> loot Book of Brohum (730174, drops 182209011) at var 6 flips to
// var 7 -> loot Old Wooden Box (700395, drops 182209012) at var 8 flips to REWARD -> turn in at
// Surt (consumes 182209012).
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

public sealed class _2093TheSecretPassage : QuestHandlerBase
{
    private const int QuestIdConst  = 2093;
    private const int SurtNpc       = 205150;
    private const int NeligorNpc    = 205159;
    private const int BuBuKhaaanNpc = 205164;
    private const int BuBuChanNpc   = 205197;
    private const int CayronNpc     = 205198;
    private const int BrohumBookNpc = 730174;
    private const int WoodenBoxNpc  = 700395;
    private const int BrohumBookItem = 182209011;
    private const int PassageKeyItem = 182209012;

    private readonly IItemDao _itemDao;

    public _2093TheSecretPassage(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        RegisterQuestDrop(engine, BrohumBookNpc, BrohumBookItem, 1, 100);
        RegisterQuestDrop(engine, WoodenBoxNpc, PassageKeyItem, 1, 100);
        engine.RegisterItemGet(BrohumBookItem, QuestId);
        engine.RegisterItemGet(PassageKeyItem, QuestId);
        engine.RegisterQuestNpc(SurtNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(NeligorNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BuBuKhaaanNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BuBuChanNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(CayronNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(BrohumBookNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(WoodenBoxNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 2091, isZoneMission: true, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId != SurtNpc) return false;
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            await RemoveQuestItemAsync(player, conn, _itemDao, PassageKeyItem, 1, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == SurtNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 0:
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                case DialogAction.SELECT_ACTION_1012:
                    await PlayQuestMovieAsync(conn, player, 397, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
                case DialogAction.SETPRO1:
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                default:
                    return false;
            }
        }

        if (targetId == NeligorNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                case DialogAction.SETPRO2:
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                default:
                    return false;
            }
        }

        if (targetId == BuBuKhaaanNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 2:
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                case DialogAction.SETPRO3:
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                default:
                    return false;
            }
        }

        if (targetId == BuBuChanNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 3:
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                case DialogAction.QUEST_SELECT when var == 4:
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                case DialogAction.SETPRO4:
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
                case DialogAction.SETPRO5:
                    return await DefaultCloseDialogAsync(env, conn, 4, 4, ct);
                case DialogAction.CHECK_USER_HAS_QUEST_ITEM:
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 4, 5, reward: false, checkOkId: 10000, checkFailId: 10001, ct);
                default:
                    return false;
            }
        }

        if (targetId == CayronNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT when var == 5:
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                case DialogAction.QUEST_SELECT when var == 7:
                    return await SendQuestDialogAsync(conn, targetObjId, 3398, ct);
                case DialogAction.SELECT_ACTION_3399:
                    await PlayQuestMovieAsync(conn, player, 398, ct);
                    await RemoveQuestItemAsync(player, conn, _itemDao, BrohumBookItem, 1, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 3399, ct);
                case DialogAction.SETPRO6:
                    return await DefaultCloseDialogAsync(env, conn, 5, 6, ct);
                case DialogAction.SETPRO8:
                    return await DefaultCloseDialogAsync(env, conn, 7, 8, ct);
                default:
                    return false;
            }
        }

        if (targetId == BrohumBookNpc)
            return var == 6 && dialog == DialogAction.USE_OBJECT;

        if (targetId == WoodenBoxNpc)
            return var == 8 && dialog == DialogAction.USE_OBJECT;

        return false;
    }

    public override async ValueTask<bool> OnItemGetAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != BrohumBookItem && itemId != PassageKeyItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var == 6)
        {
            await ChangeQuestStepAsync(conn, entry, 0, 7, toReward: false, ct);
            return true;
        }
        if (var == 8)
        {
            // Java defaultOnGetItemEvent(env, 8, 8, true): var 8 exactly flips to REWARD without a var change.
            await ChangeQuestStepAsync(conn, entry, varIdx: -1, newValue: 0, toReward: true, ct);
            return true;
        }
        return false;
    }
}
