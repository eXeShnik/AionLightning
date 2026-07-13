// Port of Java data/scripts/system/handlers/quest/daevation/_1989ASagesTeachings.java (Rhys2002).
// Start at Fermina (203771); visit the class trainer matching the player's class (203704 Boreas,
// 203705 Jumentis, 203706 Charna, 203707 Thrasymedes, 801214, 801215) - each shows a class-specific
// dialog page and SETPRO1 advances var0 0->1. Back at Fermina the var0 chain runs 1->2 (SETPRO2),
// SETPRO4 sets var0=3 and SETPRO5 sets var0=4; SELECT_QUEST_REWARD at var0 3 or 4 plays movie 105,
// zeroes DP and flips to REWARD (dialog 5). The var 3/4 QUEST_SELECT pages branch on DP >= 4000.
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

namespace Quest.Daevation;

public sealed class _1989ASagesTeachings : QuestHandlerBase
{
    private const int QuestIdConst = 1989;
    private const int Fermina      = 203771;

    public _1989ASagesTeachings(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(Fermina).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(Fermina).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(203704).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(203705).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(203706).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(203707).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(801214).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(801215).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player      = env.Player;
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);
        var entry       = player.Quests.Get(QuestId);

        if (entry is null || entry.Status == QuestStatus.NONE)
        {
            if (targetId == Fermina)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                return await SendQuestStartDialogAsync(env, conn, ct);
            }
            return false;
        }

        int var = entry.GetVar(0);

        if (entry.Status == QuestStatus.START)
        {
            var pc = player.PlayerClass;

            int? trainerPage = targetId switch
            {
                203704 => pc is PlayerClass.GLADIATOR or PlayerClass.TEMPLAR ? 1352 : 1438,
                203705 => pc is PlayerClass.ASSASSIN or PlayerClass.RANGER ? 1693 : 1779,
                203706 => pc is PlayerClass.SORCERER or PlayerClass.SPIRIT_MASTER ? 2034 : 2120,
                203707 => pc is PlayerClass.CLERIC or PlayerClass.CHANTER ? 2375 : 2461,
                801214 => pc == PlayerClass.GUNNER ? 2548 : 2568,
                801215 => pc == PlayerClass.BARD ? 2633 : 2653,
                _      => (int?)null,
            };
            if (trainerPage.HasValue)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                    return await SendQuestDialogAsync(conn, targetObjId, trainerPage.Value, ct);
                if (dialog == DialogAction.SETPRO1)
                {
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                return false;
            }

            if (targetId == Fermina)
            {
                if (dialog == DialogAction.QUEST_SELECT)
                {
                    if (var == 1) return await SendQuestDialogAsync(conn, targetObjId, 2716, ct);
                    if (var == 2) return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                    if (var == 3) return await SendQuestDialogAsync(conn, targetObjId, player.Dp < 4000 ? 3484 : 3398, ct);
                    if (var == 4) return await SendQuestDialogAsync(conn, targetObjId, player.Dp < 4000 ? 3825 : 3739, ct);
                    // Java switch fallthrough: QUEST_SELECT with an unmatched var drops into SELECT_QUEST_REWARD
                }
                if (dialog == DialogAction.QUEST_SELECT || dialog == DialogAction.SELECT_QUEST_REWARD)
                {
                    if (var == 3 || var == 4)
                    {
                        await PlayQuestMovieAsync(conn, player, 105, ct);
                        player.Dp = 0;
                        entry.Status = QuestStatus.REWARD;
                        await UpdateQuestStatusAsync(conn, entry, ct);
                        return await SendQuestDialogAsync(conn, targetObjId, 5, ct);
                    }
                    return await SendQuestEndDialogAsync(env, conn, ct);
                }
                if (dialog == DialogAction.SETPRO2)
                {
                    entry.SetVar(0, var + 1);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 3057, ct);
                }
                if (dialog == DialogAction.SETPRO4)
                {
                    entry.SetVar(0, 3);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
                if (dialog == DialogAction.SETPRO5)
                {
                    entry.SetVar(0, 4);
                    await UpdateQuestStatusAsync(conn, entry, ct);
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                }
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            if (targetId == Fermina)
                return await SendQuestEndDialogAsync(env, conn, ct);
        }
        return false;
    }
}
