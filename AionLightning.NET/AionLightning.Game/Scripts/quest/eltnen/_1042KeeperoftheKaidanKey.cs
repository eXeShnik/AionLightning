// Port of Java data/scripts/system/handlers/quest/eltnen/_1042KeeperoftheKaidanKey.java (Rhys2002, edited by xaerolt).
// Zone-mission quest, part of the Kaidan Fortress chain (1040/1300): talk to Tumblusen (203989,
// var 0->1, movie 185); loot the Strong Document Box (730342, drops item 182201026) or one of three
// Kaidan mobs; use the document (var 1->2) and hand it in at Telemachus (203901, collect-item
// check flips straight to REWARD).
using System.Collections.Generic;
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

public sealed class _1042KeeperoftheKaidanKey : QuestHandlerBase
{
    private const int QuestIdConst   = 1042;
    private const int TumblusenNpc   = 203989;
    private const int TelemachusNpc  = 203901;
    private const int DocumentBoxObj = 730342;
    private const int DocumentItem   = 182201026;

    private readonly IItemDao _itemDao;

    public _1042KeeperoftheKaidanKey(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestItem(DocumentItem, QuestId);
        RegisterQuestDrop(engine, DocumentBoxObj, DocumentItem, 1, 100);
        RegisterQuestDrop(engine, 212025, DocumentItem, 1, 100);
        RegisterQuestDrop(engine, 212029, DocumentItem, 1, 100);
        RegisterQuestDrop(engine, 212033, DocumentItem, 1, 100);
        foreach (int npc in new[] { TumblusenNpc, TelemachusNpc })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, precedingQuestId: 1040, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, (IReadOnlyCollection<int>)[1300, 1040], isZoneMission: true, ct);

    public override async ValueTask<bool> OnItemUseAsync(Player player, int itemId, GsClientConnection conn, CancellationToken ct)
    {
        if (itemId != DocumentItem) return false;
        var entry = player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START || entry.GetVar(0) != 1) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: false, ct);
        return true;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.REWARD)
            return targetId == TelemachusNpc && await SendQuestEndDialogAsync(env, conn, ct);
        if (entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);

        if (targetId == TumblusenNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 0)
                return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
            if (dialog == DialogAction.SELECT_ACTION_1012)
            {
                await PlayQuestMovieAsync(conn, player, 185, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
            }
            if (dialog == DialogAction.SETPRO1)
                return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            return false;
        }

        if (targetId == DocumentBoxObj)
            return var == 1 && dialog == DialogAction.USE_OBJECT;

        if (targetId == TelemachusNpc)
        {
            if (dialog == DialogAction.QUEST_SELECT && var == 2)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                return await CheckQuestItemsAsync(env, conn, _itemDao, 2, 2, reward: true, checkOkId: 5, checkFailId: 1438, ct);
            if (dialog == DialogAction.FINISH_DIALOG)
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            return false;
        }

        return false;
    }
}
