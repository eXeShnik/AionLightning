// Port of Java data/scripts/system/handlers/quest/morheim/_2039AlliesAmongEnemies.java (Hellboy aion4Free).
// Zone-mission chain quest (auto-started via OnLevelUpAsync/OnZoneMissionEndAsync once 2300 is
// COMPLETE). Talk 204345 (var 0->1), 204387 (var 1->2, movie 84 on SELECT_ACTION_1353), then any of
// three parallel sub-objectives at var 2 - 204411 sets var-slot 1, 204412 sets var-slot 2, 204413
// sets var-slot 3 (var 0 itself stays at 2 throughout); back at 204387, SET_SUCCEED flips straight
// to REWARD regardless of which/how many of the three sub-objectives were completed (matches Java -
// the three side vars are never actually read again). Turn in at 204388.
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

public sealed class _2039AlliesAmongEnemies : QuestHandlerBase
{
    private const int QuestIdConst = 2039;

    private static readonly int[] _npcIds = [204345, 204387, 204388, 204411, 204412, 204413];

    public _2039AlliesAmongEnemies(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnZoneMissionEnd(QuestId);
        engine.RegisterOnLevelUp(QuestId);
        foreach (int npc in _npcIds)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnZoneMissionEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnZoneMissionEndEventAsync(env, conn, ct);

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, 2300, isZoneMission: true, ct);

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
            if (targetId == 204345)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 0 && await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return var == 0 && await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
                return false;
            }

            if (targetId == 204387)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                    return false;
                }
                if (dialog == DialogAction.SELECT_ACTION_1353)
                {
                    await PlayQuestMovieAsync(conn, player, 84, ct);
                    return false;
                }
                if (dialog == DialogAction.SETPRO2)
                    return var == 1 && await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
                if (dialog == DialogAction.SET_SUCCEED)
                    return var == 2 && await DefaultCloseDialogAsync(env, conn, 2, 2, reward: true, sameNpc: false, ct);
                return false;
            }

            if (targetId == 204411)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 2 && await SendQuestDialogAsync(conn, targetObjId, 1694, ct);
                if (dialog == DialogAction.SETPRO3)
                {
                    if (var != 2) return false;
                    entry.SetVar(1, 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == 204412)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 2 && await SendQuestDialogAsync(conn, targetObjId, 1779, ct);
                if (dialog == DialogAction.SETPRO4)
                {
                    if (var != 2) return false;
                    entry.SetVar(2, 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == 204413)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return var == 2 && await SendQuestDialogAsync(conn, targetObjId, 1864, ct);
                if (dialog == DialogAction.SETPRO5)
                {
                    if (var != 2) return false;
                    entry.SetVar(3, 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == 204388)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 10002, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
