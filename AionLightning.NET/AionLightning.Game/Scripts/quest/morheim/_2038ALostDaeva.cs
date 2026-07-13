// Port of Java data/scripts/system/handlers/quest/morheim/_2038ALostDaeva.java (Hellboy aion4Free, reworked vlog).
// Zone-mission chain quest (auto-started via OnLevelUpAsync/OnZoneMissionEndAsync once 2300 is
// COMPLETE). Talk Mirka (204342, var 0->1, movie 82), use Pagimkin's Corpse (700233) at var 1 to
// advance to var 2, killing 212879 at var 2 jumps straight to var 4 (a +2 bump), back to Mirka
// (var 4) flips to REWARD while removing item 182204016. Turn in at Kvasir (204053).
// Skip vs Java: onDieEvent (plays movie 83 if the player dies at var 1/2 inside
// WONSHIKUTZS_LABORATORY_220020000) has no OnDieAsync hook in this port - flavor-only, no state
// change, so nothing progression-relevant is lost.
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

public sealed class _2038ALostDaeva : QuestHandlerBase
{
    private const int QuestIdConst = 2038;
    private const int MirkaNpc     = 204342;
    private const int KvasirNpc    = 204053;
    private const int CorpseObj    = 700233;
    private const int GhoulNpc     = 212879;
    private const int CleanupItem  = 182204016;

    private readonly IItemDao _itemDao;

    public _2038ALostDaeva(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(GhoulNpc).OnKill.Add(QuestId);
        foreach (int npc in new[] { MirkaNpc, KvasirNpc, CorpseObj })
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 2300, isZoneMission: true, ct);

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;
        if (env.TargetId != GhoulNpc || entry.GetVar(0) != 2) return false;

        await ChangeQuestStepAsync(conn, entry, 0, 4, toReward: false, ct);
        return true;
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
            if (targetId == MirkaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 0) return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                    if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    return false;
                }
                if (dialog == DialogAction.SELECT_ACTION_1012)
                {
                    if (var != 0) return false;
                    await PlayQuestMovieAsync(conn, player, 82, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
                }
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return var == 4 && await DefaultCloseDialogAsync(env, conn, _itemDao, 4, 4,
                        reward: true, sameNpc: false, giveItemId: 0, giveItemCount: 0,
                        removeItemId: CleanupItem, removeItemCount: 1, ct);
                return false;
            }

            if (targetId == CorpseObj)
            {
                if (dialog == DialogAction.USE_OBJECT && var == 1)
                    return await UseQuestObjectAsync(env, conn, 1, 2, reward: false, varNum: 0, ct);
                return false;
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
}
