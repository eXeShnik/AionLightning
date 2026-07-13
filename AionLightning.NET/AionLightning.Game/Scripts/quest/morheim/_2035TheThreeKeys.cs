// Port of Java data/scripts/system/handlers/quest/morheim/_2035TheThreeKeys.java (Hellboy aion4Free).
// Zone-mission chain quest (auto-started via OnLevelUpAsync/OnZoneMissionEndAsync once 2300 is
// COMPLETE). Talk 204317 (var 0->4, jumps straight past 1-3 - unused in this quest), then 204408:
// SELECT_ACTION_2376 plays movie 78; SETPRO5 (var 4) hands out item 182204012 and advances to var 5;
// entering world 320050000 while at var 5 bumps to var 6 (OnEnterWorldAsync); back at 204408,
// CHECK_USER_HAS_QUEST_ITEM (var 6) checks the quest_data.xml collect-item list and, on success,
// also consumes item 182204012 and flips to REWARD. Turn in at 204407.
// Java bug fixed: onDialogEvent's QUEST_SELECT case at 204408 had no break/return for var values
// other than 4/6, so it fell through into the SELECT_ACTION_2376 case and replayed movie 78 on every
// re-opened dialog at var 5 - fixed by making each dialog an independent guarded branch (SETPRO5 and
// CHECK_USER_HAS_QUEST_ITEM's incidental fallthrough into each other is likewise not reachable
// through normal client dialog ids and is not replicated).
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Morheim;

public sealed class _2035TheThreeKeys : QuestHandlerBase
{
    private const int QuestIdConst = 2035;
    private const int GuideItem    = 182204012;
    private const int EnterWorldId = 320050000;

    private static readonly int[] _npcIds = [204317, 204408, 204407];

    private readonly IItemDao _itemDao;

    public _2035TheThreeKeys(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterOnEnterWorld(QuestId);
        foreach (int npc in _npcIds)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 2300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnEnterWorldAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is not { Status: QuestStatus.START }) return false;
        if (player.Position.WorldId == EnterWorldId && entry.GetVar(0) == 5)
        {
            entry.SetVar(0, 6);
            await UpdateQuestStatusAsync(conn, entry, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        if (entry is null) return false;

        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);
        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == 204317)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1 && var == 0)
                {
                    entry.SetVar(0, 4);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == 204408)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    if (var == 6) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    return false;
                }
                if (dialog == DialogAction.SELECT_ACTION_2376)
                {
                    await PlayQuestMovieAsync(conn, player, 78, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO5)
                {
                    if (var != 4) return false;
                    if (!await GiveQuestItemAsync(player, conn, _itemDao, GuideItem, 1, ct)) return true;
                    entry.SetVar(0, 5);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM)
                {
                    if (var != 6) return false;
                    bool ok = await CheckQuestItemsAsync(env, conn, _itemDao, 6, 6, reward: true, 10000, 10001, ct);
                    if (ok) await RemoveQuestItemAsync(player, conn, _itemDao, GuideItem, 1, ct);
                    return ok;
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == 204407)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
