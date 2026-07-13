// Port of Java data/scripts/system/handlers/quest/siels_spear/_41450Steps_To_The_Spear.java (Cheatkiller).
// Talk to 205579 to start (accepting arms a 780s quest timer); relay chain 205798 (var 0->1) ->
// 205799 (var 1->2) -> 205800 (var 2->3) -> 205801 (var 3->4) -> 205579 (var 4->5), then
// CHECK_USER_HAS_QUEST_ITEM_SIMPLE at 205579 checks for collect-item(s) (var 5->6, reward); turn in
// at 205579. NPCs 730527/800280/800298 are registered for talk events but have no dialog branch in
// Java either (dead registrations - preserved as-is).
// Skip vs Java: no OnDie/OnLogOut hook exists in this port, so onDieEvent (fails the quest back to
// NONE with a QUEST_FAILED_$1 message on death while the timer is running) and onLogOutEvent
// (silently resets to NONE on disconnect) are both omitted - same simplification as
// eltnen._1033SatalocasHeart's onLogOutEvent skip and talocs_hollow._11467DeathToTheQueen's onDie
// skip. The REWARD-branch QuestService.questTimerEnd(env) call (explicit timer cancellation) is
// also skipped - no cancellation API is exposed, harmless since the timer callback already no-ops
// once status has left START (same as sarpan._21506This_End_Up).
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

namespace Quest.SielsSpear;

public sealed class _41450Steps_To_The_Spear : QuestHandlerBase
{
    private const int QuestIdConst = 41450;
    private const int RelayNpc1    = 205798;
    private const int RelayNpc2    = 205799;
    private const int RelayNpc3    = 205800;
    private const int RelayNpc4    = 205801;
    private const int StartNpc     = 205579;
    private const int TimerSeconds = 780;

    private static readonly int[] _talkNpcs = [205798, 205799, 205800, 205801, 205579, 730527, 800280, 800298];

    private readonly IItemDao _itemDao;

    public _41450Steps_To_The_Spear(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
        _itemDao = itemDao;
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnQuestTimerEnd(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        foreach (int npc in _talkNpcs)
            engine.RegisterQuestNpc(npc).OnTalk.Add(QuestId);
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
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                if (dialog == DialogAction.QUEST_ACCEPT_SIMPLE)
                {
                    StartQuestTimer(env, conn, TimerSeconds);
                    return await SendQuestStartDialogAsync(env, conn, ct);
                }
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (targetId == RelayNpc1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            if (targetId == RelayNpc2 && var == 1)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, ct);
            }
            if (targetId == RelayNpc3 && var == 2)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
                if (dialog == DialogAction.SETPRO3)
                    return await DefaultCloseDialogAsync(env, conn, 2, 3, ct);
            }
            if (targetId == RelayNpc4 && var == 3)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2034, ct);
                if (dialog == DialogAction.SETPRO4)
                    return await DefaultCloseDialogAsync(env, conn, 3, 4, ct);
            }
            if (targetId == StartNpc && var == 4)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2375, ct);
                if (dialog == DialogAction.SETPRO5)
                    return await DefaultCloseDialogAsync(env, conn, 4, 5, ct);
            }
            if (targetId == StartNpc && var == 5)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                if (dialog == DialogAction.CHECK_USER_HAS_QUEST_ITEM_SIMPLE)
                    return await CheckQuestItemsAsync(env, conn, _itemDao, 5, 6, true, 10002, 10001, ct);
            }
        }
        else if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == StartNpc)
            {
                if (dialog == DialogAction.SELECT_QUEST_REWARD)
                    return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                return await SendQuestEndDialogAsync(env, conn, ct);
            }
        }
        return false;
    }

    public override async ValueTask<bool> OnQuestTimerEndAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var > 1)
        {
            entry.Status = QuestStatus.NONE;
            entry.SetVar(0, 0);
            await UpdateQuestStatusAsync(conn, entry, ct);
            // Skip vs Java: SM_SYSTEM_MESSAGE(QUEST_FAILED_$1, questName) - no matching factory exposed; see header.
            return true;
        }
        return false;
    }
}
