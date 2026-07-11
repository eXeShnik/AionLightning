// Port of Java data/scripts/system/handlers/quest/morheim/_2411EarthSpiritWaterSpirit.java.
// Start at 204369; picking SETPRO10 (var->1) or SETPRO20 (var->2) flips to REWARD immediately,
// each choosing a different reward tier; turning in at either spirit npc (204366/204364) finishes
// with that tier (Java's sendQuestEndDialog(env, reward) two-step dialog — SELECT_QUEST_REWARD
// shows the "confirm tier" page 5+reward, SELECTED_QUEST_REWARDn/NOREWARD actually completes).
// Java stores the reward index in a shared instance field (a latent thread-safety bug for a
// singleton handler); this port derives it from the persisted var instead (var 1 -> 0, var 2 -> 1),
// which is race-free and behaviourally identical for a single player. Java only registers
// OnQuestStart (not OnTalk) for the start npc 204369; OnTalk is added here too so the quest is
// actually reachable through this engine's npc-click dispatch.
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

public sealed class _2411EarthSpiritWaterSpirit : QuestHandlerBase
{
    private const int QuestIdConst   = 2411;
    private const int StartNpc       = 204369;
    private const int EarthSpiritNpc = 204366;
    private const int WaterSpiritNpc = 204364;

    public _2411EarthSpiritWaterSpirit(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterQuestNpc(StartNpc).OnQuestStart.Add(QuestId);
        engine.RegisterQuestNpc(StartNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(EarthSpiritNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(WaterSpiritNpc).OnTalk.Add(QuestId);
    }

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        int targetId = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog = DialogActionLookup.FromId(env.DialogId);

        if (entry is null)
        {
            if (targetId != StartNpc) return false;
            if (dialog == DialogAction.QUEST_SELECT)
                return await SendQuestDialogAsync(conn, targetObjId, 4762, ct);
            return await SendQuestStartDialogAsync(env, conn, ct);
        }

        if (entry.Status == QuestStatus.START && targetId == StartNpc)
        {
            switch (dialog)
            {
                case DialogAction.QUEST_SELECT:
                    return await SendQuestDialogAsync(conn, targetObjId, 1003, ct);
                case DialogAction.SELECT_ACTION_1012:
                    return await SendQuestDialogAsync(conn, targetObjId, 1012, ct);
                case DialogAction.SELECT_ACTION_1097:
                    return await SendQuestDialogAsync(conn, targetObjId, 1097, ct);
                case DialogAction.SETPRO10:
                    await ChangeQuestStepAsync(conn, entry, 0, 1, toReward: true, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                case DialogAction.SETPRO20:
                    await ChangeQuestStepAsync(conn, entry, 0, 2, toReward: true, ct);
                    return await CloseDialogWindowAsync(conn, targetObjId, ct);
                default:
                    return false;
            }
        }

        if (entry.Status == QuestStatus.REWARD)
        {
            int var = entry.GetVar(0);
            if (targetId == EarthSpiritNpc && var == 1 && dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
            if (targetId == WaterSpiritNpc && var == 2 && dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);

            return await SendQuestEndDialogWithRewardAsync(env, conn, var - 1, ct);
        }
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
