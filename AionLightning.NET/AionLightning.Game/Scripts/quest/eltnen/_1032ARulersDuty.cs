// Port of Java data/scripts/system/handlers/quest/eltnen/_1032ARulersDuty.java (Xitanium, Ritsu, Antraxx).
// Zone-mission quest, part of the Kaidan Fortress chain (1300): use the Ruler's Medallion
// (182201001) inside the item-use area to advance var 0->4 (talk to Phomona first, var 0->1, movie
// 177); Demro drives var 1->2 and, later, var 5->reward; Lodas drives var 2->3 and, at var 4, removes
// the medallion + plays movie 49 to reach var 5; the Seau Kerubien loot object (700157) just acks
// USE_OBJECT at var 3 (comment "loot" in Java - no state change of its own).
// Skip vs Java: the 3s SM_ITEM_USAGE_ANIMATION cast delay on the medallion use is applied on the
// same tick instead - the same simplification UseQuestObjectAsync's doc comment already documents.
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

namespace Quest.Eltnen;

public sealed class _1032ARulersDuty : QuestHandlerBase
{
    private const int QuestIdConst   = 1032;
    private const int PhomonaNpc     = 203932;
    private const int DemroNpc       = 730020;
    private const int LodasNpc       = 730019;
    private const int SeauKerubienObj = 700157;
    private const int MedallionItem  = 182201001;
    private const string ItemUseZone = "LF2_ITEMUSEAREA_Q1032";

    private readonly IItemDao _itemDao;

    public _1032ARulersDuty(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestItem(MedallionItem, QuestId);
        engine.RegisterQuestNpc(PhomonaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(DemroNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LodasNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(SeauKerubienObj).OnTalk.Add(QuestId);
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, precedingQuestId: 1300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != MedallionItem) return false;
        if (!player.CurrentZones.Contains(ItemUseZone)) return false;

        var entry = player.Quests.Get(QuestId);
        if (entry is null) return false;

        entry.SetVar(0, 4);
        await UpdateQuestStatusAsync(conn, entry, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        var entry       = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (targetId == PhomonaNpc)
        {
            if (entry.Status == QuestStatus.START)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SELECT_ACTION_1013 && entry.GetVar(0) == 0)
                {
                    await PlayQuestMovieAsync(conn, player, 177, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1013, ct);
                }
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.REWARD)
            {
                if (dialog == DialogAction.USE_OBJECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == DemroNpc)
        {
            int var = entry.GetVar(0);
            if (entry.Status == QuestStatus.START && var == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.START && var == 5)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, 5, 5, reward: true, sameNpc: false, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == LodasNpc)
        {
            int var = entry.GetVar(0);
            if (entry.Status == QuestStatus.START && var == 2)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            if (entry.Status == QuestStatus.START && var == 4)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                {
                    await RemoveQuestItemAsync(player, conn, _itemDao, MedallionItem, 1, ct);
                    await PlayQuestMovieAsync(conn, player, 49, ct);
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (targetId == SeauKerubienObj && entry.Status == QuestStatus.START && entry.GetVar(0) == 3)
            return dialog == DialogAction.USE_OBJECT;

        return false;
    }
}
