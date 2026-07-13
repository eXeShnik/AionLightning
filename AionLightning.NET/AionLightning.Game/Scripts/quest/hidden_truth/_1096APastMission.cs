// Port of Java data/scripts/system/handlers/quest/hidden_truth/_1096APastMission.java
// (Hellboy, aion4Free; modified apozema). Talk to Lavirintos (203701) to advance var0->1, then
// Ludina (203852) to advance var1->2 and flip to REWARD, turn in at Pernos (790001).
// Skip vs Java: Ludina's SETPRO2 case also teleports the player to Poeta (210010000) via
// TeleportService2 — no TeleportService exists in this port (same precedent as
// quest/eltnen/_1430ATeleportationExperiment.cs). The var/status transition is kept.
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

namespace Quest.HiddenTruth;

public sealed class _1096APastMission : QuestHandlerBase
{
    private const int QuestIdConst = 1096;
    private const int LavirintosNpc = 203701;
    private const int LudinaNpc      = 203852;
    private const int PernosNpc      = 790001;

    public _1096APastMission(IDataManager dataManager, IQuestDao questDao, QuestRewardService rewardService, IItemDao itemDao)
        : base(QuestIdConst, dataManager, questDao, rewardService)
    {
    }

    public override void Register(QuestEngine engine)
    {
        engine.RegisterOnLevelUp(QuestId);
        engine.RegisterQuestNpc(LavirintosNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(LudinaNpc).OnTalk.Add(QuestId);
        engine.RegisterQuestNpc(PernosNpc).OnTalk.Add(QuestId);
    }

    public override ValueTask<bool> OnLevelUpAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
        => DefaultOnLvlUpEventAsync(env, conn, ct);

    public override async ValueTask<bool> OnDialogAsync(QuestEnv env, GsClientConnection conn, CancellationToken ct)
    {
        var entry = env.Player.Quests.Get(QuestId);
        if (entry is null) return false;

        int var         = entry.GetVar(0);
        int targetId    = env.TargetId;
        int targetObjId = env.Target?.ObjectId ?? 0;
        var dialog      = DialogActionLookup.FromId(env.DialogId);

        if (entry.Status == QuestStatus.START)
        {
            if (targetId == LavirintosNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 0)
                    return await SendQuestDialogAsync(conn, targetObjId, 1011, ct);
                if (dialog == DialogAction.SETPRO1 && var == 0)
                    return await DefaultCloseDialogAsync(env, conn, 0, 1, ct);
            }
            else if (targetId == LudinaNpc)
            {
                if (dialog == DialogAction.QUEST_SELECT && var == 1)
                    return await SendQuestDialogAsync(conn, targetObjId, 1352, ct);
                if (dialog == DialogAction.SETPRO2 && var == 1)
                    return await DefaultCloseDialogAsync(env, conn, 1, 2, reward: true, sameNpc: false, ct);
            }
            return false;
        }

        if (entry.Status == QuestStatus.REWARD && targetId == PernosNpc)
        {
            if (dialog == DialogAction.USE_OBJECT)
                return await SendQuestDialogAsync(conn, targetObjId, 1693, ct);
            return await SendQuestEndDialogAsync(env, conn, ct);
        }

        return false;
    }
}
