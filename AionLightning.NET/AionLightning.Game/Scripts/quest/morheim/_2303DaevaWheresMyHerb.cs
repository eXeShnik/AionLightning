// Port of Java data/scripts/system/handlers/quest/morheim/_2303DaevaWheresMyHerb.java.
// Talk to Bicorunerk (798082) and pick one of two paths (SETPRO10 -> hunt Daru at var 11-15,
// SETPRO20 -> hunt Ettins at var 21-25); each path's kill span flips to REWARD at its terminal
// var, then turning in shows the matching reward tier (Java's sendQuestEndDialog(env, reward)
// two-step dialog). Java tracks which path was picked in a shared instance field ("choice"); this
// port derives it from the persisted var instead (< 20 -> path 0, >= 20 -> path 1), which is
// race-free and behaviourally identical for a single player.
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Quest;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.QuestEngine;
using AionLightning.Game.QuestEngine.Handlers;
using AionLightning.Game.QuestEngine.Model;
using AionLightning.Game.Services;

namespace Quest.Morheim;

public sealed class _2303DaevaWheresMyHerb : QuestHandlerBase
{
    private const int QuestIdConst = 2303;
    private const int StartNpc     = 798082;

    private static readonly int[] _daruMobs   = [211298, 211305];
    private static readonly int[] _ettinMobs  = [211304, 211297];

    public _2303DaevaWheresMyHerb(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        foreach (int mob in _daruMobs.Concat(_ettinMobs))
            engine.RegisterQuestNpc(mob).OnKill.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var player = env.Player;
        var entry  = player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (targetId != StartNpc) return false;

        if (entry is null)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
                case DialogAction.ASK_QUEST_ACCEPT:
                    return await SendQuestDialogAsync(conn, targetObjId, 4, ct);
                case DialogAction.QUEST_ACCEPT_1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                case DialogAction.QUEST_REFUSE_1:
                    return await SendQuestDialogAsync(conn, targetObjId, 1004, ct);
                case DialogAction.SETPRO10:
                {
                    if (!await StartMissionAsync(conn, player, QuestStatus.START, ct))
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    var started = player.Quests.Get(QuestId)!;
                    await ChangeQuestStepAsync(conn, started, 0, 11, toReward: false, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
                }
                case DialogAction.SETPRO20:
                {
                    if (!await StartMissionAsync(conn, player, QuestStatus.START, ct))
                        return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                    var started = player.Quests.Get(QuestId)!;
                    await ChangeQuestStepAsync(conn, started, 0, 21, toReward: false, ct);
                    return await SendQuestDialogAsync(conn, targetObjId, 1097, ct);
                }
                case DialogAction.FINISH_DIALOG:
                    return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.START)
        {
            int var = entry.GetVar(0);
            if (dialog == DialogAction.FINISH_DIALOG)
                return await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            if (dialog == DialogAction.USE_OBJECT)
                return var == 0
                    ? await SendQuestDialogAsync(conn, targetObjId, 1003, ct)
                    : await SendQuestSelectionDialogAsync(conn, targetObjId, ct);
            if (dialog == DialogAction.SETPRO10)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 11, toReward: false, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
            }
            if (dialog == DialogAction.SETPRO20)
            {
                await ChangeQuestStepAsync(conn, entry, 0, 21, toReward: false, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 1097, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            int var = entry.GetVar(0);
            int choice = var < 20 ? 0 : 1;
            if (dialog == DialogAction.USE_OBJECT)
            {
                if (var == 15) return await SendQuestDialogAsync(conn, targetObjId, 1353, ct);
                if (var == 25) return await SendQuestDialogAsync(conn, targetObjId, 1438, ct);
                return await SendQuestDialogAsync(conn, targetObjId, 5 + choice, ct);
            }
            if (dialog == DialogAction.SELECT_QUEST_REWARD)
                return await SendQuestDialogAsync(conn, targetObjId, 5 + choice, ct);
            return await SendQuestEndDialogWithRewardAsync(env, conn, choice, ct);
        }
        return false;
    }

    public override async ValueTask<bool> OnKillAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.START) return false;

        int var = entry.GetVar(0);
        if (var is >= 11 and < 15)
            return await DefaultOnKillEventAsync(env, conn, _daruMobs, 10, 15, ct);
        if (var == 15)
            return await DefaultOnKillEventAsync(env, conn, _daruMobs, 15, reward: true, ct);
        if (var is >= 21 and < 25)
            return await DefaultOnKillEventAsync(env, conn, _ettinMobs, 20, 25, ct);
        if (var == 25)
            return await DefaultOnKillEventAsync(env, conn, _ettinMobs, 25, reward: true, ct);
        return false;
    }

    /// <summary>Java QuestHandler.sendQuestEndDialog(env, reward): SELECT_QUEST_REWARD/USE_OBJECT
    /// shows the tier-confirm page 5+reward; SELECTED_QUEST_REWARDn/NOREWARD actually completes
    /// with that reward tier.</summary>
    private async ValueTask<bool> SendQuestEndDialogWithRewardAsync(QuestEnv env, GsClientConnection conn, int rewardIndex, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null || entry.Status != QuestStatus.REWARD) return false;
        int targetObjId = env.Target?.ObjectId ?? 0;
        int dialogId = env.DialogId;

        if (dialogId >= (int)DialogAction.SELECTED_QUEST_REWARD1 && dialogId <= (int)DialogAction.SELECTED_QUEST_NOREWARD)
        {
            if (!await FinishQuestAsync(conn, env.Player, rewardIndex, ct)) return false;
            await conn.SendAsync(new SM_DIALOG_WINDOW(targetObjId, 10), ct);
            return true;
        }
        if (dialogId == (int)DialogAction.SELECT_QUEST_REWARD || dialogId == (int)DialogAction.USE_OBJECT)
            return await SendQuestDialogAsync(conn, targetObjId, 5 + rewardIndex, ct);
        return false;
    }
}
